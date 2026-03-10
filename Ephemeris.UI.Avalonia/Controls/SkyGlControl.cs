// Updated: 2026-03-10
using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Stellarography;
using static Avalonia.OpenGL.OpenGlConsts;

namespace Ephemeris.UI.Avalonia.Controls;

/// <summary>
/// Cross-platform OpenGL sky view control for Avalonia.
/// Derives from <see cref="OpenGlControlBase"/> (the Avalonia equivalent of
/// <c>OpenTK.GLControl</c>) so that the same rendering logic runs on
/// Windows, Linux, and macOS.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>OpenTK.GLControl</c> (WinForms-only), <see cref="OpenGlControlBase"/>
/// uses Avalonia's platform abstraction layer: X11/EGL on Linux, WGL on Windows,
/// and CGL on macOS — no GLFW dependency.
/// </para>
/// <para>
/// Coordinate system: +Y = zenith, +Z = north horizon, +X = east horizon.
/// Bodies are projected from azimuth/altitude onto the unit sphere:
///   x = cos(alt) × sin(az), y = sin(alt), z = cos(alt) × cos(az).
/// </para>
/// <para>
/// VAO/VBO functions (OpenGL 3.0+) and <c>glUniformMatrix4fv</c> are loaded via
/// <see cref="GlInterface.GetProcAddress"/> at init time to avoid dependency on
/// the version-specific OpenTK GL class.
/// </para>
/// </remarks>
public sealed class SkyGlControl : OpenGlControlBase
{
    // ── GL constants not in Avalonia's OpenGlConsts ───────────────────────
    private const int GlProgramPointSize  = 0x8642;
    private const int GlLineLoop          = 0x0002;
    private const int GlPoints            = 0x0000;
    private const int GlFloat             = 0x1406;
    private const int GlArrayBuffer       = 0x8892;
    private const int GlStaticDraw        = 0x88B4;
    private const int GlDynamicDraw       = 0x88E8;
    private const int GlDepthTest         = 0x0B71;
    private const int GlBlend             = 0x0BE2;
    private const int GlSrcAlpha          = 0x0302;
    private const int GlOneMinusSrcAlpha  = 0x0303;
    private const int GlVertexShader      = 0x8B31;
    private const int GlFragmentShader    = 0x8B30;
    private const int GlCompileStatus     = 0x8B81;
    private const int GlLinkStatus        = 0x8B82;

    // ── Shader source (identical to the WinForms version) ─────────────────
    private const string VertexShaderSource = """
        #version 330 core
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec4 aColor;
        layout(location = 2) in float aSize;

        uniform mat4 uMVP;

        out vec4 vColor;

        void main()
        {
            gl_Position  = uMVP * vec4(aPos, 1.0);
            gl_PointSize = aSize;
            vColor       = aColor;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        in  vec4 vColor;
        out vec4 fragColor;

        void main()
        {
            vec2 c = gl_PointCoord - vec2(0.5);
            if (dot(c, c) > 0.25)
                discard;
            fragColor = vColor;
        }
        """;

    // ── GL 3.0+ function delegates (loaded via GetProcAddress) ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GenVertexArraysDelegate(int n, int[] arrays);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void BindVertexArrayDelegate(int array);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DeleteVertexArraysDelegate(int n, int[] arrays);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GenBuffersDelegate(int n, int[] buffers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DeleteBuffersDelegate(int n, int[] buffers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void UniformMatrix4FvDelegate(int location, int count, bool transpose, float* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void BufferDataDelegate(int target, IntPtr size, float* data, int usage);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void GetBufferSubDataDelegate(int target, IntPtr offset, IntPtr size, float* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void GetShaderivDelegate(int shader, int pname, int* param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void GetProgramivDelegate(int program, int pname, int* param);

    private GenVertexArraysDelegate?  _genVertexArrays;
    private BindVertexArrayDelegate?  _bindVertexArray;
    private DeleteVertexArraysDelegate? _deleteVertexArrays;
    private GenBuffersDelegate?       _genBuffers;
    private DeleteBuffersDelegate?    _deleteBuffers;
    private UniformMatrix4FvDelegate? _uniformMatrix4fv;
    private BufferDataDelegate?       _bufferData;
    private GetBufferSubDataDelegate? _getBufferSubData;
    private GetShaderivDelegate?      _getShaderiv;
    private GetProgramivDelegate?     _getProgramiv;

    // ── GL object handles ─────────────────────────────────────────────────
    private int _shaderProgram;
    private int _starVao, _starVbo;
    private int _bodyVao, _bodyVbo;
    private int _horizonVao, _horizonVbo;
    private int _mvpLoc;
    private bool _glReady;
    private int _starCount;
    private int _bodyVertexCount;

    // ── Scene data ────────────────────────────────────────────────────────
    private IReadOnlyList<FixedStar> _stars = [];
    private readonly List<(Vector2 Screen, string Label, uint ColorArgb)> _labels = [];

    // ── Animation ─────────────────────────────────────────────────────────
    private readonly DispatcherTimer _animTimer;

    // ── View-model ────────────────────────────────────────────────────────
    private readonly SkyViewModel _vm;
    private bool _syncingFromVm;

    // ─────────────────────────────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the sky GL control bound to the provided view-model.
    /// </summary>
    /// <param name="vm">Shared observable view-model for observer state and camera.</param>
    public SkyGlControl(SkyViewModel vm)
    {
        _vm = vm;
        _vm.PropertyChanged += OnViewModelPropertyChanged;

        _animTimer          = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _animTimer.Tick    += (_, _) => _vm.AdvanceTick();
    }

    // ─────────────────────────────────────────────────────────────────────
    // OpenGL lifecycle
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Loads all GL 3.0+ function pointers via <see cref="GlInterface.GetProcAddress"/>,
    /// compiles shaders, allocates VAO/VBO objects, and uploads the horizon ring geometry.
    /// This mirrors <c>GLControl.Load</c> from the WinForms version.
    /// </remarks>
    protected override void OnOpenGlInit(GlInterface gl)
    {
        LoadExtensionFunctions(gl);

        gl.ClearColor(0.02f, 0.02f, 0.08f, 1f); // deep night sky
        gl.Enable(GlDepthTest);
        gl.Enable(GlProgramPointSize);
        gl.Enable(GlBlend);
        gl.BlendFunc(GlSrcAlpha, GlOneMinusSrcAlpha);

        CompileShaders(gl);
        InitBuffers(gl);
        _stars   = StarCatalog.LoadBuiltIn();
        _glReady = true;
        RequestNextFrameRendering();
    }

    /// <inheritdoc/>
    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        if (!_glReady) return;

        gl.DeleteProgram(_shaderProgram);

        _deleteVertexArrays!(1, [_starVao]);
        _deleteBuffers!(1, [_starVbo]);
        _deleteVertexArrays!(1, [_bodyVao]);
        _deleteBuffers!(1, [_bodyVbo]);
        _deleteVertexArrays!(1, [_horizonVao]);
        _deleteBuffers!(1, [_horizonVbo]);

        _glReady = false;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Called by Avalonia each time the control needs to redraw.
    /// Uploads scene vertex data each frame (stars + bodies) then renders.
    /// Uses <see cref="RequestNextFrameRendering"/> to drive animation when playing.
    /// </remarks>
    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (!_glReady) return;

        var (w, h) = ((int)Bounds.Width, (int)Bounds.Height);

        // Upload scene vertex data for the current simulation time
        double jd = TimeZoneUtils.ToJulianDay(_vm.SimTime);
        UploadStarVertices(gl, jd);
        UploadBodyVertices(gl, jd);

        gl.Viewport(0, 0, w, h);
        gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        gl.UseProgram(_shaderProgram);

        float[] mvp = BuildMvp(w, h);
        unsafe
        {
            fixed (float* p = mvp)
                _uniformMatrix4fv!(_mvpLoc, 1, false, p);
        }

        // Draw stars
        _bindVertexArray!(_starVao);
        gl.DrawArrays(GlPoints, 0, _starCount);

        // Draw Sun, Moon, planets (rendered on top)
        _bindVertexArray(_bodyVao);
        gl.DrawArrays(GlPoints, 0, _bodyVertexCount);

        // Draw horizon ring
        _bindVertexArray(_horizonVao);
        gl.DrawArrays(GlLineLoop, 0, 360);

        _bindVertexArray(0);

        UpdateLabelPositions(gl, mvp, w, h);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GL function pointer loading
    // ─────────────────────────────────────────────────────────────────────

    private void LoadExtensionFunctions(GlInterface gl)
    {
        _genVertexArrays  = Load<GenVertexArraysDelegate>(gl, "glGenVertexArrays");
        _bindVertexArray  = Load<BindVertexArrayDelegate>(gl, "glBindVertexArray");
        _deleteVertexArrays = Load<DeleteVertexArraysDelegate>(gl, "glDeleteVertexArrays");
        _genBuffers       = Load<GenBuffersDelegate>(gl, "glGenBuffers");
        _deleteBuffers    = Load<DeleteBuffersDelegate>(gl, "glDeleteBuffers");
        _uniformMatrix4fv = Load<UniformMatrix4FvDelegate>(gl, "glUniformMatrix4fv");
        _bufferData       = Load<BufferDataDelegate>(gl, "glBufferData");
        _getBufferSubData = Load<GetBufferSubDataDelegate>(gl, "glGetBufferSubData");
        _getShaderiv      = Load<GetShaderivDelegate>(gl, "glGetShaderiv");
        _getProgramiv     = Load<GetProgramivDelegate>(gl, "glGetProgramiv");
    }

    private static T Load<T>(GlInterface gl, string name) where T : Delegate
    {
        var ptr = gl.GetProcAddress(name);
        if (ptr == IntPtr.Zero)
            throw new InvalidOperationException($"OpenGL entry point '{name}' not found. " +
                "The context must support OpenGL 3.0 or later.");
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Shader compilation
    // ─────────────────────────────────────────────────────────────────────

    private void CompileShaders(GlInterface gl)
    {
        _shaderProgram = CreateProgram(gl, _getShaderiv!, _getProgramiv!, VertexShaderSource, FragmentShaderSource);
        _mvpLoc = gl.GetUniformLocation(_shaderProgram, "uMVP");
    }

    private static int CreateProgram(GlInterface gl,
        GetShaderivDelegate getShaderiv,
        GetProgramivDelegate getProgramiv,
        string vertSrc, string fragSrc)
    {
        int vert = CompileShader(gl, getShaderiv, GlVertexShader, vertSrc);
        int frag = CompileShader(gl, getShaderiv, GlFragmentShader, fragSrc);

        int prog = gl.CreateProgram();
        gl.AttachShader(prog, vert);
        gl.AttachShader(prog, frag);
        gl.LinkProgram(prog);

        int linked;
        unsafe { getProgramiv(prog, GlLinkStatus, &linked); }

        if (linked == 0)
            throw new InvalidOperationException("Shader link failed: " + gl.GetProgramInfoLog(prog));

        gl.DetachShader(prog, vert);
        gl.DetachShader(prog, frag);
        gl.DeleteShader(vert);
        gl.DeleteShader(frag);

        return prog;
    }

    private static int CompileShader(GlInterface gl,
        GetShaderivDelegate getShaderiv,
        int type, string src)
    {
        int shader = gl.CreateShader(type);
        gl.ShaderSource(shader, src);
        gl.CompileShader(shader);

        int compiled;
        unsafe { getShaderiv(shader, GlCompileStatus, &compiled); }

        if (compiled == 0)
            throw new InvalidOperationException(
                $"Shader compile failed (type={type}): " + gl.GetShaderInfoLog(shader));

        return shader;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Buffer setup
    // ─────────────────────────────────────────────────────────────────────

    private void InitBuffers(GlInterface gl)
    {
        var arr = new int[1];

        // Stars VAO/VBO
        _genVertexArrays!(1, arr); _starVao = arr[0];
        _genBuffers!(1, arr);      _starVbo = arr[0];
        SetupVaoLayout(gl, _starVao, _starVbo);

        // Bodies VAO/VBO
        _genVertexArrays!(1, arr); _bodyVao = arr[0];
        _genBuffers!(1, arr);      _bodyVbo = arr[0];
        SetupVaoLayout(gl, _bodyVao, _bodyVbo);

        // Horizon ring VAO/VBO (pos only)
        _genVertexArrays!(1, arr); _horizonVao = arr[0];
        _genBuffers!(1, arr);      _horizonVbo = arr[0];
        _bindVertexArray!(_horizonVao);
        gl.BindBuffer(GlArrayBuffer, _horizonVbo);
        gl.VertexAttribPointer(0, 3, GlFloat, 0, 3 * sizeof(float), IntPtr.Zero);
        gl.EnableVertexAttribArray(0);
        BuildHorizonRing();
        _bindVertexArray(0);
    }

    /// <summary>
    /// Configures the standard VAO layout used by stars and body buffers:
    /// location 0 = vec3 position, 1 = vec4 colour, 2 = float pointSize.
    /// Stride = 32 bytes (8 floats).
    /// </summary>
    private void SetupVaoLayout(GlInterface gl, int vao, int vbo)
    {
        const int stride = (3 + 4 + 1) * sizeof(float); // 32 bytes
        _bindVertexArray!(vao);
        gl.BindBuffer(GlArrayBuffer, vbo);
        gl.VertexAttribPointer(0, 3, GlFloat, 0, stride, IntPtr.Zero);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 4, GlFloat, 0, stride, (IntPtr)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(2, 1, GlFloat, 0, stride, (IntPtr)(7 * sizeof(float)));
        gl.EnableVertexAttribArray(2);
        _bindVertexArray(0);
    }

    private void BuildHorizonRing()
    {
        const int segments = 360;
        var verts = new float[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            double azRad = double.DegreesToRadians(i);
            verts[i * 3]     = (float)Math.Sin(azRad);
            verts[i * 3 + 1] = 0.005f;
            verts[i * 3 + 2] = (float)Math.Cos(azRad);
        }

        unsafe
        {
            fixed (float* p = verts)
                _bufferData!(GlArrayBuffer, (IntPtr)(verts.Length * sizeof(float)), p, GlStaticDraw);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Scene data upload
    // ─────────────────────────────────────────────────────────────────────

    private void UploadStarVertices(GlInterface gl, double jd)
    {
        const int floatsPerVertex = 8;
        var buffer = new List<float>(_stars.Count * floatsPerVertex);

        foreach (var star in _stars)
        {
            var eq = star.AtEpoch(jd);
            var hz = ObserverGeometry.EquatorialToHorizontal(
                eq.RightAscension, eq.Declination, jd, _vm.Longitude, _vm.Latitude);

            if (hz.Altitude < -5.0) continue;

            var (x, y, z) = AzAltToUnitSphere(hz.Azimuth, hz.Altitude);
            float size = Math.Clamp(7f - (float)star.Magnitude, 1f, 8f);
            var (r, g, b) = SpectralColor(star.SpectralType, (float)star.Magnitude);

            buffer.Add(x); buffer.Add(y); buffer.Add(z);
            buffer.Add(r); buffer.Add(g); buffer.Add(b); buffer.Add(1f);
            buffer.Add(size);
        }

        _starCount = buffer.Count / floatsPerVertex;
        float[] data = [.. buffer];

        _bindVertexArray!(_starVao);
        gl.BindBuffer(GlArrayBuffer, _starVbo);
        UploadVertexBuffer(data, GlDynamicDraw);
        _bindVertexArray(0);
    }

    private void UploadBodyVertices(GlInterface gl, double jd)
    {
        const int floatsPerVertex = 8;
        var buffer = new List<float>(12 * floatsPerVertex);
        _labels.Clear();

        int year  = _vm.SimTime.Year;
        int month = _vm.SimTime.Month;
        int day   = _vm.SimTime.Day;
        double hour = _vm.SimTime.Hour + _vm.SimTime.Minute / 60.0 + _vm.SimTime.Second / 3600.0;

        var sun = EphemerisCalculator.GetSunPosition(year, month, day, hour, _vm.Longitude, _vm.Latitude);
        AddBodyVertex(buffer, sun.Azimuth, sun.Altitude, 1.0f, 0.97f, 0.8f, 16f, "Sun");

        var moon = EphemerisCalculator.GetMoonPosition(year, month, day, hour, _vm.Longitude, _vm.Latitude);
        float moonPhase = (float)(moon.Illumination ?? 0.5);
        AddBodyVertex(buffer, moon.Azimuth, moon.Altitude,
            0.8f + 0.2f * moonPhase, 0.8f + 0.2f * moonPhase, 0.8f, 12f, "Moon");

        AddPlanet(buffer, "Mercury", 0.7f, 0.7f, 0.7f, 6f);
        AddPlanet(buffer, "Venus",   1.0f, 0.95f, 0.7f, 8f);
        AddPlanet(buffer, "Mars",    1.0f, 0.4f, 0.2f, 7f);
        AddPlanet(buffer, "Jupiter", 0.9f, 0.8f, 0.6f, 9f);
        AddPlanet(buffer, "Saturn",  0.85f, 0.8f, 0.5f, 8f);

        _bodyVertexCount = buffer.Count / floatsPerVertex;
        float[] data = [.. buffer];

        _bindVertexArray!(_bodyVao);
        gl.BindBuffer(GlArrayBuffer, _bodyVbo);
        UploadVertexBuffer(data, GlDynamicDraw);
        _bindVertexArray(0);
    }

    private void AddPlanet(List<float> buffer, string name, float r, float g, float b, float size)
    {
        try
        {
            var obs = EphemerisCalculator.GetPlanetPosition(
                name, _vm.SimTime, TimeZoneInfo.Utc.Id, _vm.Longitude, _vm.Latitude);
            AddBodyVertex(buffer, obs.Azimuth, obs.Altitude, r, g, b, size, name);
        }
        catch
        {
            // Skip bodies that can't be computed
        }
    }

    private void AddBodyVertex(List<float> buffer,
        double azimuth, double altitude,
        float r, float g, float b, float size, string label)
    {
        var (x, y, z) = AzAltToUnitSphere(azimuth, altitude);
        buffer.Add(x); buffer.Add(y); buffer.Add(z);
        buffer.Add(r); buffer.Add(g); buffer.Add(b); buffer.Add(1f);
        buffer.Add(size);

        uint argb = (255u << 24) | ((uint)(r * 255) << 16) | ((uint)(g * 255) << 8) | (uint)(b * 255);
        _labels.Add((Vector2.Zero, label, argb));
    }

    private void UploadVertexBuffer(float[] data, int usage)
    {
        unsafe
        {
            fixed (float* p = data)
                _bufferData!(GlArrayBuffer, (IntPtr)(data.Length * sizeof(float)), p, usage);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // MVP matrix (System.Numerics, row-major — passed with transpose=false
    // because the matrix is pre-transposed before upload)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the model-view-projection matrix as a column-major float[16] array
    /// ready for <c>glUniformMatrix4fv(…, transpose=false, …)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="System.Numerics.Matrix4x4"/> stores values in row-major order,
    /// whereas GLSL <c>mat4</c> uniforms expect column-major layout when
    /// <c>transpose</c> is <c>false</c>. To avoid an extra transpose flag,
    /// we transpose the matrix manually before flattening to the float array.
    /// </para>
    /// </remarks>
    private float[] BuildMvp(int width, int height)
    {
        float aspect = width > 0 && height > 0 ? (float)width / height : 1f;
        float fovRad = float.DegreesToRadians(_vm.FovDeg);

        var proj = Matrix4x4.CreatePerspectiveFieldOfView(fovRad, aspect, 0.01f, 10f);

        float yawRad   = float.DegreesToRadians(_vm.Yaw);
        float pitchRad = float.DegreesToRadians(_vm.Pitch);

        var forward = new Vector3(
            MathF.Cos(pitchRad) * MathF.Sin(yawRad),
            MathF.Sin(pitchRad),
            MathF.Cos(pitchRad) * MathF.Cos(yawRad));

        var view = Matrix4x4.CreateLookAt(Vector3.Zero, forward, Vector3.UnitY);
        var mvp  = Matrix4x4.Transpose(view * proj); // transpose for column-major upload

        // Flatten column-major (now row-major of the transposed) into float[16]
        return
        [
            mvp.M11, mvp.M12, mvp.M13, mvp.M14,
            mvp.M21, mvp.M22, mvp.M23, mvp.M24,
            mvp.M31, mvp.M32, mvp.M33, mvp.M34,
            mvp.M41, mvp.M42, mvp.M43, mvp.M44,
        ];
    }

    private void UpdateLabelPositions(GlInterface gl, float[] mvp, int width, int height)
    {
        if (_bodyVertexCount == 0 || width == 0 || height == 0) return;

        // Bind body VBO to read back vertex positions for screen-space label placement
        gl.BindBuffer(GlArrayBuffer, _bodyVbo);
        int floatCount = _bodyVertexCount * 8;
        var data = new float[floatCount];

        unsafe
        {
            fixed (float* p = data)
                _getBufferSubData!(GlArrayBuffer, IntPtr.Zero, (IntPtr)(floatCount * sizeof(float)), p);
        }

        gl.BindBuffer(GlArrayBuffer, 0);

        var m = new Matrix4x4(
            mvp[0], mvp[1], mvp[2], mvp[3],
            mvp[4], mvp[5], mvp[6], mvp[7],
            mvp[8], mvp[9], mvp[10], mvp[11],
            mvp[12], mvp[13], mvp[14], mvp[15]);

        for (int i = 0; i < Math.Min(_bodyVertexCount, _labels.Count); i++)
        {
            var worldPos = new Vector4(data[i * 8], data[i * 8 + 1], data[i * 8 + 2], 1f);
            var clipPos  = Vector4.Transform(worldPos, m);

            if (clipPos.W <= 0)
            {
                _labels[i] = (new Vector2(-1, -1), _labels[i].Label, _labels[i].ColorArgb);
                continue;
            }

            float ndcX    = clipPos.X / clipPos.W;
            float ndcY    = clipPos.Y / clipPos.W;
            float screenX = (ndcX + 1f) * 0.5f * width;
            float screenY = (1f - ndcY) * 0.5f * height;
            _labels[i] = (new Vector2(screenX, screenY), _labels[i].Label, _labels[i].ColorArgb);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Coordinate helpers (identical to WinForms version)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts azimuth/altitude (degrees) to a unit-sphere position.
    /// +Y = zenith, +Z = north, +X = east.
    /// </summary>
    private static (float X, float Y, float Z) AzAltToUnitSphere(double az, double alt)
    {
        double azRad  = double.DegreesToRadians(az);
        double altRad = double.DegreesToRadians(alt);
        return (
            (float)(Math.Cos(altRad) * Math.Sin(azRad)),
            (float)Math.Sin(altRad),
            (float)(Math.Cos(altRad) * Math.Cos(azRad)));
    }

    /// <summary>Returns an approximate RGB colour for a star from its spectral class.</summary>
    private static (float R, float G, float B) SpectralColor(string spectralType, float magnitude)
    {
        char cls = spectralType.Length > 0 ? spectralType[0] : 'G';
        float dim = Math.Clamp(1f - (magnitude - 1f) / 8f, 0.3f, 1f);
        return cls switch
        {
            'O' => (0.6f * dim, 0.7f * dim, 1.0f * dim),
            'B' => (0.7f * dim, 0.8f * dim, 1.0f * dim),
            'A' => (0.9f * dim, 0.9f * dim, 1.0f * dim),
            'F' => (1.0f * dim, 1.0f * dim, 0.9f * dim),
            'G' => (1.0f * dim, 0.95f * dim, 0.7f * dim),
            'K' => (1.0f * dim, 0.7f * dim, 0.4f * dim),
            'M' => (1.0f * dim, 0.4f * dim, 0.2f * dim),
            _   => (0.9f * dim, 0.9f * dim, 0.9f * dim),
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // ViewModel property-change synchronisation
    // ─────────────────────────────────────────────────────────────────────

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _syncingFromVm = true;
            try
            {
                switch (e.PropertyName)
                {
                    case nameof(SkyViewModel.Playing):
                        if (_vm.Playing) _animTimer.Start(); else _animTimer.Stop();
                        break;

                    case nameof(SkyViewModel.SimTime):
                    case nameof(SkyViewModel.Longitude):
                    case nameof(SkyViewModel.Latitude):
                    case nameof(SkyViewModel.Yaw):
                    case nameof(SkyViewModel.Pitch):
                    case nameof(SkyViewModel.FovDeg):
                        RequestNextFrameRendering();
                        break;
                }
            }
            finally { _syncingFromVm = false; }
        });
    }

    /// <summary>
    /// Public access for the label list so the enclosing <see cref="SkyViewWindow"/>
    /// can composite text labels on top of the GL surface via Avalonia's Canvas.
    /// </summary>
    internal IReadOnlyList<(Vector2 Screen, string Label, uint ColorArgb)> Labels => _labels;
}
