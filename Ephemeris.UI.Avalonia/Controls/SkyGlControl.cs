// Updated: 2026-03-22
using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Heliology;
using Ephemeris.Selenography;
using Ephemeris.Stellarography;
using Ephemeris.UI.Models;

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
    // ── GL constants not in Avalonia's GlInterface ────────────────────────
    private const int GL_COLOR_BUFFER_BIT  = 0x4000;
    private const int GL_DEPTH_BUFFER_BIT  = 0x0100;
    private const int GlProgramPointSize  = 0x8642;
    private const int GlLines             = 0x0001;
    private const int GlLineLoop          = 0x0002;
    private const int GlLineStrip         = 0x0003;
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

    // ── Shader sources for line rendering (no point-sprite logic) ─────────

    /// <summary>Vertex shader for line primitives; no gl_PointSize output.</summary>
    private const string LineVertexShaderSource = """
        #version 330 core
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec4 aColor;

        uniform mat4 uMVP;

        out vec4 vColor;

        void main()
        {
            gl_Position = uMVP * vec4(aPos, 1.0);
            vColor      = aColor;
        }
        """;

    /// <summary>Fragment shader for line primitives; outputs colour directly without gl_PointCoord clipping.</summary>
    private const string LineFragmentShaderSource = """
        #version 330 core
        in  vec4 vColor;
        out vec4 fragColor;

        void main()
        {
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
    private unsafe delegate void GetShaderivDelegate(int shader, int pname, int* param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void GetProgramivDelegate(int program, int pname, int* param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void BlendFuncDelegate(int sfactor, int dfactor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DetachShaderDelegate(int program, int shader);

    private GenVertexArraysDelegate?  _genVertexArrays;
    private BindVertexArrayDelegate?  _bindVertexArray;
    private DeleteVertexArraysDelegate? _deleteVertexArrays;
    private GenBuffersDelegate?       _genBuffers;
    private DeleteBuffersDelegate?    _deleteBuffers;
    private UniformMatrix4FvDelegate? _uniformMatrix4fv;
    private BufferDataDelegate?       _bufferData;
    private GetShaderivDelegate?      _getShaderiv;
    private GetProgramivDelegate?     _getProgramiv;
    private BlendFuncDelegate?        _blendFunc;
    private DetachShaderDelegate?     _detachShader;

    // ── GL object handles ─────────────────────────────────────────────────
    private int _shaderProgram;
    private int _lineProgram;
    private int _starVao, _starVbo;
    private int _bodyVao, _bodyVbo;
    private int _horizonVao, _horizonVbo;
    private int _mazzarothVao, _mazzarothVbo;
    private int _constVao, _constVbo;   // constellation lines
    private int _pathVao, _pathVbo;     // Sun/Moon path arcs
    private int _mvpLoc;
    private int _lineMvpLoc;
    private bool _glReady;
    private int _starCount;
    private int _bodyVertexCount;
    private int _mazzarothVertexCount;
    private int _constVertexCount;
    private int _pathVertexCount;

    // ── Scene data ────────────────────────────────────────────────────────
    private IReadOnlyList<FixedStar> _stars = [];
    private Dictionary<string, FixedStar> _starByName = [];  // cached for constellation lookup
    private readonly List<(Vector2 Screen, string Label, uint ColorArgb)> _labels = [];
    private readonly List<Vector3> _bodyWorldPos = [];

    // ── Thread-safe label snapshot (written GL thread, read UI thread) ────
    private volatile (Vector2 Screen, string Label, uint ColorArgb)[]? _labelSnapshot;

    // ── Animation ─────────────────────────────────────────────────────────
    private readonly DispatcherTimer _animTimer;

    // ── View-model ────────────────────────────────────────────────────────
    private readonly SkyViewModel _vm;

// ── Display toggle properties ─────────────────────────────────────────

    private bool _showConstellations;
    private bool _showStarLabels;
    private bool _showPlanetLabels = true;
    private bool _showHorizonGrid = true;
    private double _starMagnitudeLimit = 5.5;
    private bool _showSunPath;
    private bool _showMoonPath;

    /// <summary>
    /// When <see langword="true"/>, draws thin lines connecting the stars of prominent
    /// constellations. Default is <see langword="false"/>.
    /// </summary>
    public bool ShowConstellations
    {
        get => _showConstellations;
        set { _showConstellations = value; Dispatcher.UIThread.Post(RequestNextFrameRendering); }
    }

    /// <summary>
    /// When <see langword="true"/>, overlays the common name of each star brighter than
    /// magnitude 2.0. Default is <see langword="false"/>.
    /// </summary>
    public bool ShowStarLabels
    {
        get => _showStarLabels;
        set { _showStarLabels = value; Dispatcher.UIThread.Post(RequestNextFrameRendering); }
    }

    /// <summary>
    /// When <see langword="true"/>, shows name labels next to each planet and the Sun/Moon.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool ShowPlanetLabels
    {
        get => _showPlanetLabels;
        set { _showPlanetLabels = value; Dispatcher.UIThread.Post(RequestNextFrameRendering); }
    }

    /// <summary>
    /// When <see langword="true"/>, draws the horizon ring. Default is <see langword="true"/>.
    /// </summary>
    public bool ShowHorizonGrid
    {
        get => _showHorizonGrid;
        set { _showHorizonGrid = value; Dispatcher.UIThread.Post(RequestNextFrameRendering); }
    }

    /// <summary>
    /// Stars with visual magnitude ≤ this limit are rendered.
    /// Lower = fewer (brighter) stars; higher = more (fainter) stars.
    /// Valid range: 0.0 – 7.0. Default is 5.5.
    /// </summary>
    public double StarMagnitudeLimit
    {
        get => _starMagnitudeLimit;
        set { _starMagnitudeLimit = value; Dispatcher.UIThread.Post(RequestNextFrameRendering); }
    }

    /// <summary>
    /// When <see langword="true"/>, draws the Sun's altitude arc for the current simulation day
    /// as a sequence of yellow line segments. Default is <see langword="false"/>.
    /// </summary>
    public bool ShowSunPath
    {
        get => _showSunPath;
        set { _showSunPath = value; Dispatcher.UIThread.Post(RequestNextFrameRendering); }
    }

    /// <summary>
    /// When <see langword="true"/>, draws the Moon's altitude arc for the current simulation day
    /// as a sequence of silver-blue line segments. Default is <see langword="false"/>.
    /// </summary>
    public bool ShowMoonPath
    {
        get => _showMoonPath;
        set { _showMoonPath = value; Dispatcher.UIThread.Post(RequestNextFrameRendering); }
    }

    // ── Constellation line data ───────────────────────────────────────────
    // Each entry is (star1CommonName, star2CommonName). Only pairs whose stars are
    // both present in the built-in catalog will be rendered.
    private static readonly (string S1, string S2)[] s_constellationPairs =
    [
        // Orion
        ("Betelgeuse", "Bellatrix"),
        ("Betelgeuse", "Alnilam"),
        ("Bellatrix",  "Alnilam"),
        ("Alnilam",    "Alnitak"),
        ("Alnilam",    "Rigel"),
        ("Alnitak",    "Saiph"),
        ("Rigel",      "Saiph"),
        // Ursa Major (Big Dipper)
        ("Dubhe",      "Merak"),
        ("Merak",      "Phecda"),
        ("Phecda",     "Mizar"),
        ("Mizar",      "Alkaid"),
        ("Dubhe",      "Alkaid"),
        // Cassiopeia
        ("Caph",       "Schedar"),
        // Scorpius
        ("Antares",    "Acrab"),
        ("Antares",    "Larawag"),
        ("Shaula",     "Lesath"),
        ("Acrab",      "Larawag"),
        // Leo
        ("Regulus",    "Algieba"),
        ("Algieba",    "Zosma"),
        // Taurus
        ("Aldebaran",  "Elnath"),
        ("Aldebaran",  "Alcyone"),
        // Gemini
        ("Castor",     "Pollux"),
        ("Castor",     "Alhena"),
        ("Pollux",     "Alhena"),
        // Southern Cross (Crux)
        ("Acrux",      "Gacrux"),
        ("Acrux",      "Mimosa"),
        // Perseus
        ("Mirfak",     "Algol"),
        // Auriga
        ("Capella",    "Menkalinan"),
        ("Menkalinan", "Elnath"),
        ("Capella",    "Elnath"),
        // Sagittarius
        ("Kaus Australis", "Kaus Media"),
        ("Kaus Media",     "Kaus Borealis"),
        ("Kaus Australis", "Nunki"),
        // Centaurus
        ("Hadar",      "Eta Cen"),
        ("Eta Cen",    "Muhlifain"),
        // Pegasus (Great Square)
        ("Markab",     "Scheat"),
        ("Scheat",     "Alpheratz"),
        ("Alpheratz",  "Algenib"),
        ("Algenib",    "Markab"),
        // Cygnus
        ("Deneb",      "Aljanah"),
        // Virgo
        ("Spica",      "Porrima"),
        ("Porrima",    "Vindemiatrix"),
        // Andromeda
        ("Alpheratz",  "Mirach"),
        // Boötes
        ("Arcturus",   "Izar"),
    ];

    // ── Mazzaroth overlay toggle ──────────────────────────────────────────

    private bool _showMazzarothOverlay;

    /// <summary>
    /// When <see langword="true"/>, renders the 12 Mazzaroth (zodiac) constellation
    /// regions as colored bands along the ecliptic with their Hebrew names.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool ShowMazzarothOverlay
    {
        get => _showMazzarothOverlay;
        set
        {
            if (_showMazzarothOverlay == value) return;
            _showMazzarothOverlay = value;
            Dispatcher.UIThread.Post(RequestNextFrameRendering);
        }
    }

    // ── Mazzaroth constellation data ──────────────────────────────────────

    private static readonly (double StartLon, string Hebrew, float R, float G, float B)[] MazzarothBands =
    [
        (  0, "טָלֶה / Taleh (Aries)",      1.0f, 0.5f, 0.5f),
        ( 30, "שׁוֹר / Shor (Taurus)",       1.0f, 0.7f, 0.3f),
        ( 60, "תְּאוֹמִים / Teomim (Gemini)", 1.0f, 1.0f, 0.4f),
        ( 90, "סַרְטָן / Sartan (Cancer)",   0.4f, 1.0f, 0.6f),
        (120, "אַרְיֵה / Aryeh (Leo)",        1.0f, 0.6f, 0.2f),
        (150, "בְּתוּלָה / Betulah (Virgo)",  0.6f, 1.0f, 0.6f),
        (180, "מֹאזְנַיִם / Moznayim (Libra)", 0.5f, 0.9f, 1.0f),
        (210, "עַקְרָב / Akrav (Scorpio)",    0.9f, 0.3f, 0.3f),
        (240, "קֶשֶׁת / Keshet (Sagittarius)", 0.8f, 0.5f, 1.0f),
        (270, "גְּדִי / Gedi (Capricorn)",    0.6f, 0.8f, 0.5f),
        (300, "דְּלִי / Deli (Aquarius)",     0.4f, 0.7f, 1.0f),
        (330, "דָּגִים / Dagim (Pisces)",     0.6f, 0.6f, 1.0f),
    ];

    // ── Simulation override ───────────────────────────────────────────────
    private SimulationOverride? _override;

    /// <summary>
    /// Optional simulation overrides applied during rendering.
    /// When set, <see cref="SimulationOverride.SunAltitudeOffsetDegrees"/> shifts the Sun's
    /// rendered altitude, and <see cref="SimulationOverride.MotionFrozen"/> prevents the
    /// animation timer from advancing <see cref="SkyViewModel.SimTime"/>.
    /// </summary>
    public SimulationOverride? Override
    {
        get => _override;
        set
        {
            _override = value;
            RequestNextFrameRendering();
        }
    }

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
        _animTimer.Tick    += (_, _) =>
        {
            if (_override?.MotionFrozen == true) return;
            if (_override?.ReverseDaylightDirection == true)
                _vm.RewindTick();
            else
                _vm.AdvanceTick();
        };
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
        _blendFunc!(GlSrcAlpha, GlOneMinusSrcAlpha);

        CompileShaders(gl);
        InitBuffers(gl);
        _stars     = StarCatalog.LoadBuiltIn();
        _starByName = _stars.ToDictionary(s => s.CommonName, StringComparer.OrdinalIgnoreCase);
        _glReady   = true;
        RequestNextFrameRendering();
    }

    /// <inheritdoc/>
    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        if (!_glReady) return;

        gl.DeleteProgram(_shaderProgram);
        gl.DeleteProgram(_lineProgram);

        // Batch delete all VAOs and VBOs in a single call each
        _deleteVertexArrays!(6, [_starVao, _bodyVao, _horizonVao, _mazzarothVao, _constVao, _pathVao]);
        _deleteBuffers!(6, [_starVbo, _bodyVbo, _horizonVbo, _mazzarothVbo, _constVbo, _pathVbo]);

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
        // UploadBodyVertices must run before UploadStarVertices because it clears the label lists
        UploadBodyVertices(gl, jd);
UploadStarVertices(gl, jd);
        UploadConstellationVertices(gl, jd);
        UploadPathVertices(gl, jd);
        if (_showMazzarothOverlay)
            UploadMazzarothVertices(gl, jd);

        gl.Viewport(0, 0, w, h);
        gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        float[] mvp = BuildMvp(w, h);

        // ── Point-sprite pass (stars + bodies) ────────────────────────────
        gl.UseProgram(_shaderProgram);
        unsafe
        {
            fixed (float* p = mvp)
                _uniformMatrix4fv!(_mvpLoc, 1, false, p);
        }

        _bindVertexArray!(_starVao);
        gl.DrawArrays(GlPoints, 0, _starCount);

        _bindVertexArray(_bodyVao);
        gl.DrawArrays(GlPoints, 0, _bodyVertexCount);

        // ── Line pass (constellation lines, paths, horizon ring) ──────────
        gl.UseProgram(_lineProgram);
        unsafe
        {
            fixed (float* p = mvp)
                _uniformMatrix4fv!(_lineMvpLoc, 1, false, p);
        }

        if (_showHorizonGrid)
        {
            _bindVertexArray(_horizonVao);
            gl.DrawArrays(GlLineLoop, 0, 360);
        }

        if (_showConstellations && _constVertexCount > 0)
        {
            _bindVertexArray(_constVao);
            gl.DrawArrays(GlLines, 0, _constVertexCount);
        }

        if (_pathVertexCount > 0)
        {
            _bindVertexArray(_pathVao);
            gl.DrawArrays(GlLines, 0, _pathVertexCount);
        }

        // Draw Mazzaroth ecliptic overlay
        if (_showMazzarothOverlay && _mazzarothVertexCount > 0)
        {
            _bindVertexArray(_mazzarothVao);
            gl.DrawArrays(GlLines, 0, _mazzarothVertexCount);
        }

        _bindVertexArray(0);

        UpdateLabelPositions(mvp, w, h);
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
        _getShaderiv      = Load<GetShaderivDelegate>(gl, "glGetShaderiv");
        _getProgramiv     = Load<GetProgramivDelegate>(gl, "glGetProgramiv");
        _blendFunc        = Load<BlendFuncDelegate>(gl, "glBlendFunc");
        _detachShader     = Load<DetachShaderDelegate>(gl, "glDetachShader");
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
    // GlInterface string helpers (Avalonia's raw API uses nint / void*)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls <c>glGetUniformLocation</c>, marshalling <paramref name="name"/>
    /// to a null-terminated UTF-8 native pointer as Avalonia's raw API requires.
    /// </summary>
    private static unsafe int GlGetUniformLocation(GlInterface gl, int program, string name)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* p = bytes)
            return gl.GetUniformLocation(program, (nint)p);
    }

    /// <summary>
    /// Calls <c>glShaderSource</c>, marshalling <paramref name="src"/> to the
    /// raw <c>(int shader, int count, nint** strings, nint* lengths)</c> form
    /// that Avalonia's <see cref="GlInterface"/> exposes.
    /// </summary>
    private static unsafe void GlShaderSource(GlInterface gl, int shader, string src)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(src);
        int len = bytes.Length;
        fixed (byte* p = bytes)
        {
            byte* ptr = p;
            gl.ShaderSource(shader, 1, (nint)(&ptr), (nint)(&len));
        }
    }

    /// <summary>
    /// Reads the shader info log via the raw
    /// <c>GetShaderInfoLog(int, int, out int, void*)</c> overload exposed by Avalonia.
    /// </summary>
    private static unsafe string GlGetShaderInfoLog(GlInterface gl, int shader)
    {
        const int bufSize = 4096;
        var buf = new byte[bufSize];
        fixed (byte* p = buf)
        {
            gl.GetShaderInfoLog(shader, bufSize, out int len, p);
            return System.Text.Encoding.UTF8.GetString(buf, 0, Math.Max(0, len));
        }
    }

    /// <summary>
    /// Reads the program info log via the raw
    /// <c>GetProgramInfoLog(int, int, out int, void*)</c> overload exposed by Avalonia.
    /// </summary>
    private static unsafe string GlGetProgramInfoLog(GlInterface gl, int program)
    {
        const int bufSize = 4096;
        var buf = new byte[bufSize];
        fixed (byte* p = buf)
        {
            gl.GetProgramInfoLog(program, bufSize, out int len, p);
            return System.Text.Encoding.UTF8.GetString(buf, 0, Math.Max(0, len));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Shader compilation
    // ─────────────────────────────────────────────────────────────────────

    private void CompileShaders(GlInterface gl)
    {
        _shaderProgram = CreateProgram(gl, _getShaderiv!, _getProgramiv!, _detachShader!, VertexShaderSource, FragmentShaderSource);
        _mvpLoc        = GlGetUniformLocation(gl, _shaderProgram, "uMVP");

        _lineProgram   = CreateProgram(gl, _getShaderiv!, _getProgramiv!, _detachShader!, LineVertexShaderSource, LineFragmentShaderSource);
        _lineMvpLoc    = GlGetUniformLocation(gl, _lineProgram, "uMVP");
    }

    private static int CreateProgram(GlInterface gl,
        GetShaderivDelegate getShaderiv,
        GetProgramivDelegate getProgramiv,
        DetachShaderDelegate detachShader,
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
            throw new InvalidOperationException("Shader link failed: " + GlGetProgramInfoLog(gl, prog));

        detachShader(prog, vert);
        detachShader(prog, frag);
        gl.DeleteShader(vert);
        gl.DeleteShader(frag);

        return prog;
    }

    private static int CompileShader(GlInterface gl,
        GetShaderivDelegate getShaderiv,
        int type, string src)
    {
        int shader = gl.CreateShader(type);
        GlShaderSource(gl, shader, src);
        gl.CompileShader(shader);

        int compiled;
        unsafe { getShaderiv(shader, GlCompileStatus, &compiled); }

        if (compiled == 0)
            throw new InvalidOperationException(
                $"Shader compile failed (type={type}): " + GlGetShaderInfoLog(gl, shader));

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

        // Horizon ring VAO/VBO (pos only, for line-loop)
        _genVertexArrays!(1, arr); _horizonVao = arr[0];
        _genBuffers!(1, arr);      _horizonVbo = arr[0];
        _bindVertexArray!(_horizonVao);
        gl.BindBuffer(GlArrayBuffer, _horizonVbo);
        gl.VertexAttribPointer(0, 3, GlFloat, 0, 3 * sizeof(float), IntPtr.Zero);
        gl.EnableVertexAttribArray(0);
        BuildHorizonRing();
        _bindVertexArray(0);

// Mazzaroth overlay VAO/VBO (same 8-float layout as stars/bodies)
        _genVertexArrays!(1, arr); _mazzarothVao = arr[0];
        _genBuffers!(1, arr);      _mazzarothVbo = arr[0];
        SetupVaoLayout(gl, _mazzarothVao, _mazzarothVbo);
// Constellation lines VAO/VBO (pos + color, for GL_LINES)
        _genVertexArrays!(1, arr); _constVao = arr[0];
        _genBuffers!(1, arr);      _constVbo = arr[0];
        SetupLineVaoLayout(gl, _constVao, _constVbo);

        // Path overlay VAO/VBO (pos + color, for GL_LINES)
        _genVertexArrays!(1, arr); _pathVao = arr[0];
        _genBuffers!(1, arr);      _pathVbo = arr[0];
        SetupLineVaoLayout(gl, _pathVao, _pathVbo);
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

    /// <summary>
    /// Configures the VAO layout used by line buffers (constellation lines, path arcs):
    /// location 0 = vec3 position, 1 = vec4 colour.
    /// Stride = 28 bytes (7 floats).
    /// </summary>
    private void SetupLineVaoLayout(GlInterface gl, int vao, int vbo)
    {
        const int stride = (3 + 4) * sizeof(float); // 28 bytes
        _bindVertexArray!(vao);
        gl.BindBuffer(GlArrayBuffer, vbo);
        gl.VertexAttribPointer(0, 3, GlFloat, 0, stride, IntPtr.Zero);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 4, GlFloat, 0, stride, (IntPtr)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);
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
            // Apply user-configurable magnitude limit
            if (star.Magnitude > _starMagnitudeLimit) continue;

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

            // Add star label for bright stars when ShowStarLabels is enabled
            if (_showStarLabels && star.Magnitude <= 2.0 && !string.IsNullOrEmpty(star.CommonName))
            {
                uint argb = (255u << 24) | ((uint)(r * 200) << 16) | ((uint)(g * 200) << 8) | (uint)(b * 200);
                _labels.Add((Vector2.Zero, star.CommonName, argb));
                _bodyWorldPos.Add(new Vector3(x, y, z));
            }
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
        _bodyWorldPos.Clear();

        int year  = _vm.SimTime.Year;
        int month = _vm.SimTime.Month;
        int day   = _vm.SimTime.Day;
        double hour = _vm.SimTime.Hour + _vm.SimTime.Minute / 60.0 + _vm.SimTime.Second / 3600.0;

        var sun = EphemerisCalculator.GetSunPosition(year, month, day, hour, _vm.Longitude, _vm.Latitude);
        double sunAltitude = sun.Altitude + (_override?.SunAltitudeOffsetDegrees ?? 0.0);
        AddBodyVertex(buffer, sun.Azimuth, sunAltitude, 1.0f, 0.97f, 0.8f, 16f, _showPlanetLabels ? "Sun" : null);

        var moon = EphemerisCalculator.GetMoonPosition(year, month, day, hour, _vm.Longitude, _vm.Latitude);
        float moonPhase = (float)(moon.Illumination ?? 0.5);
        AddBodyVertex(buffer, moon.Azimuth, moon.Altitude,
            0.8f + 0.2f * moonPhase, 0.8f + 0.2f * moonPhase, 0.8f, 12f, _showPlanetLabels ? "Moon" : null);

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
            AddBodyVertex(buffer, obs.Azimuth, obs.Altitude, r, g, b, size, _showPlanetLabels ? name : null);
        }
        catch
        {
            // Skip bodies that can't be computed
        }
    }

    private void AddBodyVertex(List<float> buffer,
        double azimuth, double altitude,
        float r, float g, float b, float size, string? label)
    {
        var (x, y, z) = AzAltToUnitSphere(azimuth, altitude);
        buffer.Add(x); buffer.Add(y); buffer.Add(z);
        buffer.Add(r); buffer.Add(g); buffer.Add(b); buffer.Add(1f);
        buffer.Add(size);

        uint argb = (255u << 24) | ((uint)(r * 255) << 16) | ((uint)(g * 255) << 8) | (uint)(b * 255);
        _bodyWorldPos.Add(new Vector3(x, y, z));
        _labels.Add((Vector2.Zero, label ?? string.Empty, argb));
    }

    /// <summary>
    /// Computes and uploads the Mazzaroth (ecliptic) overlay geometry into the Mazzaroth VBO.
    /// </summary>
    /// <param name="gl">Active GL interface.</param>
    /// <param name="jd">Julian Day for the current epoch (used to project to horizontal coords).</param>
    /// <remarks>
    /// Algorithm:
    /// <list type="number">
    ///   <item>Compute mean obliquity of the ecliptic: ε = 23.439291° − 0.013004° × T.</item>
    ///   <item>Sample the ecliptic at 1° intervals (λ = 0°…360°, β = 0°).</item>
    ///   <item>
    ///     Convert ecliptic → equatorial (Meeus Ch. 13):
    ///     RA = atan2(sin(λ)·cos(ε), cos(λ)),  Dec = asin(sin(ε)·sin(λ))
    ///   </item>
    ///   <item>Convert equatorial → horizontal via <see cref="ObserverGeometry.EquatorialToHorizontal"/>.</item>
    ///   <item>Build line-segment pairs for each adjacent sample, coloured per 30° Mazzaroth band.</item>
    /// </list>
    /// A label entry is added for the midpoint of each Mazzaroth band visible above the horizon.
    /// </remarks>
    private void UploadMazzarothVertices(GlInterface gl, double jd)
    {
        const int floatsPerVertex = 8;
        const int samplesPerBand  = 30; // one sample per degree within each 30° band
        var buffer = new List<float>(12 * samplesPerBand * 2 * floatsPerVertex);

        double T       = Ephemeris.Chronology.TimeUtils.JulianCentury(jd);
        double epsilon = 23.439291 - 0.013004 * T; // mean obliquity in degrees
        double epsRad  = double.DegreesToRadians(epsilon);

        // Precompute horizontal coordinates for ecliptic longitudes 0–360°
        // (one extra sample so we can always close the last segment)
        const int totalSamples = 361;
        var hzPoints = new (double Az, double Alt, bool Above)[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            double lam = double.DegreesToRadians(i);
            double ra  = double.RadiansToDegrees(Math.Atan2(Math.Sin(lam) * Math.Cos(epsRad), Math.Cos(lam)));
            double dec = double.RadiansToDegrees(Math.Asin(Math.Sin(epsRad) * Math.Sin(lam)));
            ra = Ephemeris.Chronology.TimeUtils.NormalizeDegrees(ra);

            var hz = ObserverGeometry.EquatorialToHorizontal(
                ra, dec, jd, _vm.Longitude, _vm.Latitude, applyRefraction: false);
            hzPoints[i] = (hz.Azimuth, hz.Altitude, hz.Altitude > -5.0);
        }

        // For each Mazzaroth band, emit line segments and a label at the midpoint
        for (int band = 0; band < MazzarothBands.Length; band++)
        {
            var (startLon, hebrew, r, g, b) = MazzarothBands[band];
            int startIdx = (int)startLon;
            int endIdx   = startIdx + samplesPerBand;

            // Emit adjacent-sample line segments for this band
            for (int i = startIdx; i < endIdx; i++)
            {
                var p0 = hzPoints[i];
                var p1 = hzPoints[i + 1];
                if (!p0.Above && !p1.Above) continue;
                EmitEclipticVertex(buffer, p0.Az, p0.Alt, r, g, b);
                EmitEclipticVertex(buffer, p1.Az, p1.Alt, r, g, b);
            }

            // Label at midpoint (longitude startLon + 15)
            int midIdx = startIdx + 15;
            if (midIdx < totalSamples && hzPoints[midIdx].Above)
            {
                var mp = hzPoints[midIdx];
                var (lx, ly, lz) = AzAltToUnitSphere(mp.Az, mp.Alt);
                uint argb = (200u << 24)
                          | ((uint)(r * 255) << 16)
                          | ((uint)(g * 255) << 8)
                          | (uint)(b * 255);
                _bodyWorldPos.Add(new Vector3(lx, ly, lz));
                _labels.Add((Vector2.Zero, hebrew, argb));
            }
        }

        _mazzarothVertexCount = buffer.Count / floatsPerVertex;
        float[] data = [.. buffer];

        _bindVertexArray!(_mazzarothVao);
        gl.BindBuffer(GlArrayBuffer, _mazzarothVbo);
        UploadVertexBuffer(data, GlDynamicDraw);
        _bindVertexArray(0);
    }

    /// <summary>Emits one ecliptic-line vertex (position + colour + point-size = 8 floats).</summary>
    private static void EmitEclipticVertex(
        List<float> buffer, double azimuth, double altitude, float r, float g, float b)
    {
        var (x, y, z) = AzAltToUnitSphere(azimuth, altitude);
        buffer.Add(x); buffer.Add(y); buffer.Add(z);
        buffer.Add(r); buffer.Add(g); buffer.Add(b); buffer.Add(0.5f);
        buffer.Add(1f); // stride-filler (pointSize, unused for lines)
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
    /// Uploads constellation line vertices to the constellation VBO.
    /// Each constellation line is a pair of unit-sphere positions with a dim blue-white colour.
    /// Only lines where both endpoints are above the horizon are included.
    /// </summary>
    /// <remarks>
    /// Vertex format: vec3 position + vec4 colour (stride = 28 bytes) — used by the line shader.
    /// </remarks>
    private void UploadConstellationVertices(GlInterface gl, double jd)
    {
        const int floatsPerVertex = 7; // pos(3) + color(4)
        var buffer = new List<float>(s_constellationPairs.Length * 2 * floatsPerVertex);

        foreach (var (s1Name, s2Name) in s_constellationPairs)
        {
            if (!_starByName.TryGetValue(s1Name, out var star1) ||
                !_starByName.TryGetValue(s2Name, out var star2))
                continue;

            var eq1 = star1.AtEpoch(jd);
            var hz1 = ObserverGeometry.EquatorialToHorizontal(eq1.RightAscension, eq1.Declination, jd, _vm.Longitude, _vm.Latitude);
            if (hz1.Altitude < -5.0) continue;

            var eq2 = star2.AtEpoch(jd);
            var hz2 = ObserverGeometry.EquatorialToHorizontal(eq2.RightAscension, eq2.Declination, jd, _vm.Longitude, _vm.Latitude);
            if (hz2.Altitude < -5.0) continue;

            var (x1, y1, z1) = AzAltToUnitSphere(hz1.Azimuth, hz1.Altitude);
            var (x2, y2, z2) = AzAltToUnitSphere(hz2.Azimuth, hz2.Altitude);

            // Dim blue-white colour for constellation lines
            buffer.Add(x1); buffer.Add(y1); buffer.Add(z1);
            buffer.Add(0.4f); buffer.Add(0.5f); buffer.Add(0.8f); buffer.Add(0.6f);

            buffer.Add(x2); buffer.Add(y2); buffer.Add(z2);
            buffer.Add(0.4f); buffer.Add(0.5f); buffer.Add(0.8f); buffer.Add(0.6f);
        }

        _constVertexCount = buffer.Count / floatsPerVertex;
        float[] data = [.. buffer];

        _bindVertexArray!(_constVao);
        gl.BindBuffer(GlArrayBuffer, _constVbo);
        UploadVertexBuffer(data, GlDynamicDraw);
        _bindVertexArray(0);
    }

    /// <summary>
    /// Uploads Sun and/or Moon daily path arc vertices to the path VBO.
    /// Computes 24 hourly positions for the current simulation day and connects them as line segments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sun path is drawn in yellow (1.0, 0.9, 0.0, 0.7).
    /// Moon path is drawn in silver-blue (0.7, 0.8, 1.0, 0.7).
    /// </para>
    /// <para>
    /// Vertex format: vec3 position + vec4 colour (stride = 28 bytes) — used by the line shader.
    /// Adjacent points are connected using GL_LINES pairs, skipping any point below the horizon.
    /// </para>
    /// </remarks>
    private void UploadPathVertices(GlInterface gl, double jd)
    {
        const int floatsPerVertex = 7; // pos(3) + color(4)
        var buffer = new List<float>(48 * floatsPerVertex);

        if (!_showSunPath && !_showMoonPath)
        {
            _pathVertexCount = 0;
            _bindVertexArray!(_pathVao);
            gl.BindBuffer(GlArrayBuffer, _pathVbo);
            UploadVertexBuffer([], GlDynamicDraw);
            _bindVertexArray(0);
            return;
        }

        // Use start-of-day (midnight UTC) to span the full day arc
        var dayStart = _vm.SimTime.Date;
        int year  = dayStart.Year;
        int month = dayStart.Month;
        int day   = dayStart.Day;

        if (_showSunPath)
            AppendBodyPath(buffer, year, month, day, isSun: true);

        if (_showMoonPath)
            AppendBodyPath(buffer, year, month, day, isSun: false);

        _pathVertexCount = buffer.Count / floatsPerVertex;
        float[] data = [.. buffer];

        _bindVertexArray!(_pathVao);
        gl.BindBuffer(GlArrayBuffer, _pathVbo);
        UploadVertexBuffer(data, GlDynamicDraw);
        _bindVertexArray(0);
    }

    /// <summary>
    /// Appends 24 hourly path vertices for the Sun or Moon to <paramref name="buffer"/>
    /// using GL_LINES pairs (each visible consecutive segment is two vertices).
    /// </summary>
    /// <param name="buffer">Target vertex buffer.</param>
    /// <param name="year">UTC year of the day being traced.</param>
    /// <param name="month">UTC month of the day being traced.</param>
    /// <param name="day">UTC day of the day being traced.</param>
    /// <param name="isSun"><see langword="true"/> for the Sun (yellow), <see langword="false"/> for the Moon (silver-blue).</param>
    private void AppendBodyPath(List<float> buffer, int year, int month, int day, bool isSun)
    {
        (float r, float g, float b, float a) color = isSun
            ? (1.0f, 0.9f, 0.0f, 0.7f)
            : (0.7f, 0.8f, 1.0f, 0.7f);

        // Compute 25 positions (0h through 24h = next midnight) for the full-day arc
        const int steps = 24;
        var positions = new (float x, float y, float z, bool visible)[steps + 1];
        for (int h = 0; h <= steps; h++)
        {
            double hour = h;
            CelestialObservation obs = isSun
                ? EphemerisCalculator.GetSunPosition(year, month, day, hour, _vm.Longitude, _vm.Latitude)
                : EphemerisCalculator.GetMoonPosition(year, month, day, hour, _vm.Longitude, _vm.Latitude);

            bool visible = obs.Altitude > -5.0;
            var (x, y, z) = AzAltToUnitSphere(obs.Azimuth, obs.Altitude);
            positions[h] = (x, y, z, visible);
        }

        // Emit GL_LINES pairs for each consecutive visible segment
        for (int h = 0; h < steps; h++)
        {
            var (x1, y1, z1, v1) = positions[h];
            var (x2, y2, z2, v2) = positions[h + 1];
            if (!v1 || !v2) continue;

            buffer.Add(x1); buffer.Add(y1); buffer.Add(z1);
            buffer.Add(color.r); buffer.Add(color.g); buffer.Add(color.b); buffer.Add(color.a);

            buffer.Add(x2); buffer.Add(y2); buffer.Add(z2);
            buffer.Add(color.r); buffer.Add(color.g); buffer.Add(color.b); buffer.Add(color.a);
        }
    }

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

    private void UpdateLabelPositions(float[] mvp, int width, int height)
    {
        if (_bodyWorldPos.Count == 0 || width == 0 || height == 0)
        {
            _labelSnapshot = [];
            return;
        }

        var m = new Matrix4x4(
            mvp[0], mvp[1], mvp[2], mvp[3],
            mvp[4], mvp[5], mvp[6], mvp[7],
            mvp[8], mvp[9], mvp[10], mvp[11],
            mvp[12], mvp[13], mvp[14], mvp[15]);

        for (int i = 0; i < Math.Min(_bodyWorldPos.Count, _labels.Count); i++)
        {
            var clipPos = Vector4.Transform(new Vector4(_bodyWorldPos[i], 1f), m);

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

        // Atomic reference write — UI thread reads _labelSnapshot safely
        _labelSnapshot = [.. _labels];
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
        });
    }

    /// <summary>
    /// Thread-safe snapshot of screen-space label positions, updated each render frame.
    /// Read from the UI thread (e.g. a label-refresh timer); written atomically by the GL thread.
    /// </summary>
    internal IReadOnlyList<(Vector2 Screen, string Label, uint ColorArgb)> Labels =>
        (IReadOnlyList<(Vector2, string, uint)>?)_labelSnapshot ?? [];
}
