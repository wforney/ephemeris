// Updated: 2026-03-10
using System.ComponentModel;
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Stellarography;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Ephemeris.UI;

/// <summary>
/// 3D sky view form showing stars, the Sun, Moon, and planets rendered on a
/// virtual celestial hemisphere using OpenGL, with a SkiaSharp label overlay.
/// </summary>
/// <remarks>
/// <para>
/// Coordinate system: +Y = zenith, +Z = north horizon, +X = east horizon.
/// Bodies are projected from azimuth/altitude onto the unit sphere:
///   x = cos(alt) × sin(az), y = sin(alt), z = cos(alt) × cos(az).
/// </para>
/// <para>
/// Navigation: hold left mouse button and drag to rotate the view;
/// mouse wheel zooms; Space = play/pause animation; ← / → keys step
/// by one day; F key resets to the current date/time.
/// </para>
/// </remarks>
public sealed class SkyViewForm : Form
{
    // ── View-model (observable state) ────────────────────────────────────
    private readonly SkyViewModel _vm;
    private bool _syncingFromVm; // prevents feedback loops during control ↔ vm sync

    // ── Mouse drag state ─────────────────────────────────────────────────
    private bool _dragging;
    private Point _lastMouse;

    // ── Animation timer ───────────────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _animTimer;

    // ── OpenGL objects ────────────────────────────────────────────────────
    private GLControl _gl = null!;
    private int _shaderProgram;
    private int _starVao, _starVbo;
    private int _bodyVao, _bodyVbo;
    private int _horizonVao, _horizonVbo;
    private int _mvpLoc;
    private bool _glReady;
    private int _starCount;
    private int _bodyVertexCount;

    // ── Label overlay ─────────────────────────────────────────────────────
    private Panel _overlayPanel = null!;
    private readonly List<(PointF Screen, string Label, SKColor Color)> _labels = [];

    // ── Toolbar controls ─────────────────────────────────────────────────
    private DateTimePicker _datePicker = null!;
    private NumericUpDown _lonPicker = null!;
    private NumericUpDown _latPicker = null!;
    private ToolStripButton _playBtn = null!;

    // ── Star data (load once) ─────────────────────────────────────────────
    private IReadOnlyList<FixedStar> _stars = [];

    // ─────────────────────────────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the sky view form with the given observer position and initial time.
    /// </summary>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="initialTime">Initial UTC simulation time (defaults to <see cref="DateTime.UtcNow"/>).</param>
    public SkyViewForm(double longitude = 0.0, double latitude = 51.5, DateTime initialTime = default)
    {
        _vm = new SkyViewModel(longitude, latitude, initialTime);
        _vm.PropertyChanged += OnViewModelPropertyChanged;

        Text          = "Ephemeris — Sky View";
        Width         = 1024;
        Height        = 768;
        MinimumSize   = new Size(640, 480);

        BuildToolbar();
        BuildGlControl();
        BuildOverlayPanel();
        KeyPreview = true;
        KeyDown   += OnKeyDown;

        _animTimer       = new System.Windows.Forms.Timer { Interval = 50 };
        _animTimer.Tick += (_, _) => _vm.AdvanceTick();
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI construction
    // ─────────────────────────────────────────────────────────────────────

    private void BuildToolbar()
    {
        var toolbar = new ToolStrip { Dock = DockStyle.Top };

        // Date/time picker
        toolbar.Items.Add(new ToolStripLabel("Date/Time UTC:"));
        _datePicker = new DateTimePicker
        {
            Format       = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            Value        = _vm.SimTime,
            Width        = 160,
        };
        _datePicker.ValueChanged += (_, _) =>
        {
            if (!_syncingFromVm) _vm.SimTime = _datePicker.Value.ToUniversalTime();
        };
        var dateHost = new ToolStripControlHost(_datePicker);
        toolbar.Items.Add(dateHost);

        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(new ToolStripLabel("Lon:"));

        _lonPicker = new NumericUpDown
        {
            Minimum = -180m, Maximum = 180m, DecimalPlaces = 2, Increment = 1m,
            Value = (decimal)_vm.Longitude, Width = 75,
        };
        _lonPicker.ValueChanged += (_, _) =>
        {
            if (!_syncingFromVm) _vm.Longitude = (double)_lonPicker.Value;
        };
        toolbar.Items.Add(new ToolStripControlHost(_lonPicker));

        toolbar.Items.Add(new ToolStripLabel("Lat:"));
        _latPicker = new NumericUpDown
        {
            Minimum = -90m, Maximum = 90m, DecimalPlaces = 2, Increment = 1m,
            Value = (decimal)_vm.Latitude, Width = 75,
        };
        _latPicker.ValueChanged += (_, _) =>
        {
            if (!_syncingFromVm) _vm.Latitude = (double)_latPicker.Value;
        };
        toolbar.Items.Add(new ToolStripControlHost(_latPicker));

        toolbar.Items.Add(new ToolStripSeparator());

        _playBtn = new ToolStripButton("▶ Play") { Name = "btnPlay" };
        _playBtn.Click += (_, _) => _vm.PlayPauseCommand.Execute(null);
        toolbar.Items.Add(_playBtn);

        var nowBtn = new ToolStripButton("Now");
        nowBtn.Click += (_, _) => _vm.ResetToNowCommand.Execute(null);
        toolbar.Items.Add(nowBtn);

        Controls.Add(toolbar);
    }

    private void BuildGlControl()
    {
        var settings = new GLControlSettings
        {
            API            = OpenTK.Windowing.Common.ContextAPI.OpenGL,
            APIVersion     = new Version(3, 3),
            Profile        = OpenTK.Windowing.Common.ContextProfile.Core,
            Flags          = OpenTK.Windowing.Common.ContextFlags.ForwardCompatible,
        };

        _gl = new GLControl(settings)
        {
            Dock = DockStyle.Fill,
        };

        _gl.Load   += OnGlLoad;
        _gl.Paint  += OnGlPaint;
        _gl.Resize += OnGlResize;

        _gl.MouseDown  += OnMouseDown;
        _gl.MouseUp    += OnMouseUp;
        _gl.MouseMove  += OnMouseMove;
        _gl.MouseWheel += OnMouseWheel;

        Controls.Add(_gl);
    }

    private void BuildOverlayPanel()
    {
        _overlayPanel = new Panel
        {
            Dock        = DockStyle.Fill,
            BackColor   = Color.Transparent,
        };
        _overlayPanel.Paint += OnOverlayPaint;
        Controls.Add(_overlayPanel);
        _overlayPanel.BringToFront();
    }

    // ─────────────────────────────────────────────────────────────────────
    // OpenGL initialisation
    // ─────────────────────────────────────────────────────────────────────

    private void OnGlLoad(object? sender, EventArgs e)
    {
        _gl.MakeCurrent();

        GL.ClearColor(0.02f, 0.02f, 0.08f, 1f); // deep night sky
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ProgramPointSize);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        CompileShaders();
        InitBuffers();

        _glReady = true;
        LoadStars();
        RefreshScene();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Shaders
    // ─────────────────────────────────────────────────────────────────────

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
            // Discard fragments outside the unit circle to get round points.
            vec2 c = gl_PointCoord - vec2(0.5);
            if (dot(c, c) > 0.25)
                discard;
            fragColor = vColor;
        }
        """;

    private const string HorizonFragmentShaderSource = """
        #version 330 core
        out vec4 fragColor;
        void main() { fragColor = vec4(0.2, 0.6, 0.2, 0.7); }
        """;

    private void CompileShaders()
    {
        _shaderProgram = CreateProgram(VertexShaderSource, FragmentShaderSource);
        _mvpLoc = GL.GetUniformLocation(_shaderProgram, "uMVP");
    }

    private static int CreateProgram(string vertSrc, string fragSrc)
    {
        int vert = CompileShader(ShaderType.VertexShader, vertSrc);
        int frag = CompileShader(ShaderType.FragmentShader, fragSrc);

        int prog = GL.CreateProgram();
        GL.AttachShader(prog, vert);
        GL.AttachShader(prog, frag);
        GL.LinkProgram(prog);

        GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0)
            throw new InvalidOperationException("Shader link failed: " + GL.GetProgramInfoLog(prog));

        GL.DetachShader(prog, vert);
        GL.DetachShader(prog, frag);
        GL.DeleteShader(vert);
        GL.DeleteShader(frag);

        return prog;
    }

    private static int CompileShader(ShaderType type, string src)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, src);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int compiled);
        if (compiled == 0)
            throw new InvalidOperationException($"Shader compile failed ({type}): " + GL.GetShaderInfoLog(shader));

        return shader;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Buffer setup
    // ─────────────────────────────────────────────────────────────────────

    private void InitBuffers()
    {
        // Stars VAO/VBO
        _starVao = GL.GenVertexArray();
        _starVbo = GL.GenBuffer();
        SetupVaoLayout(_starVao, _starVbo);

        // Bodies VAO/VBO
        _bodyVao = GL.GenVertexArray();
        _bodyVbo = GL.GenBuffer();
        SetupVaoLayout(_bodyVao, _bodyVbo);

        // Horizon ring VAO/VBO
        _horizonVao = GL.GenVertexArray();
        _horizonVbo = GL.GenBuffer();
        GL.BindVertexArray(_horizonVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _horizonVbo);
        // horizon ring: just vec3 positions (no color/size per vertex)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        BuildHorizonRing();
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Sets up the standard VAO layout: location 0 = vec3 pos, 1 = vec4 color, 2 = float pointSize.
    /// Stride = 32 bytes (3+4+1 floats).
    /// </summary>
    private static void SetupVaoLayout(int vao, int vbo)
    {
        const int stride = (3 + 4 + 1) * sizeof(float); // 32 bytes
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        // pos: 3 floats at offset 0
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        // color: 4 floats at offset 12
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        // point size: 1 float at offset 28
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, 7 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.BindVertexArray(0);
    }

    private void BuildHorizonRing()
    {
        const int segments = 360;
        var verts = new float[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            double azRad = double.DegreesToRadians(i);
            // Slightly above the mathematical horizon to avoid z-fighting
            verts[i * 3]     = (float)Math.Sin(azRad);
            verts[i * 3 + 1] = 0.005f;
            verts[i * 3 + 2] = (float)Math.Cos(azRad);
        }

        GL.BufferData(BufferTarget.ArrayBuffer,
            verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Data loading & scene refresh
    // ─────────────────────────────────────────────────────────────────────

    private void LoadStars()
    {
        _stars = StarCatalog.LoadBuiltIn();
    }

    private void RefreshScene()
    {
        if (!_glReady) return;
        _gl.MakeCurrent();

        double jd = TimeZoneUtils.ToJulianDay(_vm.SimTime);

        UploadStarVertices(jd);
        UploadBodyVertices(jd);
        _gl.Invalidate();
        _overlayPanel.Invalidate();
    }

    private void InvalidateScene()
    {
        if (!_glReady) return;
        _gl.MakeCurrent();
        RefreshScene();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Star vertex upload
    // ─────────────────────────────────────────────────────────────────────

    private void UploadStarVertices(double jd)
    {
        const int floatsPerVertex = 8; // x,y,z, r,g,b,a, size

        var buffer = new List<float>(_stars.Count * floatsPerVertex);

        foreach (var star in _stars)
        {
            // Get precessed equatorial coords at current epoch
            var eq = star.AtEpoch(jd);

            // Convert RA/Dec → azimuth/altitude from observer
            var hz = ObserverGeometry.EquatorialToHorizontal(
                eq.RightAscension, eq.Declination, jd, _vm.Longitude, _vm.Latitude);

            // Only render above-horizon stars (and slightly below for atmospheric refraction)
            if (hz.Altitude < -5.0) continue;

            var (x, y, z) = AzAltToUnitSphere(hz.Azimuth, hz.Altitude);

            // Magnitude → point size (brighter = bigger; mag ≤ 1 → 6px, mag 6 → 1px)
            float size = Math.Clamp(7f - (float)star.Magnitude, 1f, 8f);

            // Spectral type → approximate color
            var (r, g, b) = SpectralColor(star.SpectralType, (float)star.Magnitude);

            buffer.Add(x); buffer.Add(y); buffer.Add(z);
            buffer.Add(r); buffer.Add(g); buffer.Add(b); buffer.Add(1f);
            buffer.Add(size);
        }

        _starCount = buffer.Count / floatsPerVertex;
        float[] data = [.. buffer];

        GL.BindVertexArray(_starVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _starVbo);
        GL.BufferData(BufferTarget.ArrayBuffer,
            data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);
        GL.BindVertexArray(0);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Body vertex upload (Sun, Moon, planets)
    // ─────────────────────────────────────────────────────────────────────

    private void UploadBodyVertices(double jd)
    {
        const int floatsPerVertex = 8;
        var buffer = new List<float>(12 * floatsPerVertex);
        _labels.Clear();

        int year  = _vm.SimTime.Year;
        int month = _vm.SimTime.Month;
        int day   = _vm.SimTime.Day;
        double hour = _vm.SimTime.Hour + _vm.SimTime.Minute / 60.0 + _vm.SimTime.Second / 3600.0;

        // Sun
        var sun = EphemerisCalculator.GetSunPosition(year, month, day, hour, _vm.Longitude, _vm.Latitude);
        AddBodyVertex(buffer, sun.Azimuth, sun.Altitude, 1.0f, 0.97f, 0.8f, 16f, "Sun");

        // Moon
        var moon = EphemerisCalculator.GetMoonPosition(year, month, day, hour, _vm.Longitude, _vm.Latitude);
        float moonPhase = (float)(moon.Illumination ?? 0.5);
        AddBodyVertex(buffer, moon.Azimuth, moon.Altitude,
            0.8f + 0.2f * moonPhase, 0.8f + 0.2f * moonPhase, 0.8f, 12f, "Moon");

        // Planets
        AddPlanet(buffer, "Mercury", year, month, day, hour, 0.7f, 0.7f, 0.7f, 6f);
        AddPlanet(buffer, "Venus",   year, month, day, hour, 1.0f, 0.95f, 0.7f, 8f);
        AddPlanet(buffer, "Mars",    year, month, day, hour, 1.0f, 0.4f, 0.2f, 7f);
        AddPlanet(buffer, "Jupiter", year, month, day, hour, 0.9f, 0.8f, 0.6f, 9f);
        AddPlanet(buffer, "Saturn",  year, month, day, hour, 0.85f, 0.8f, 0.5f, 8f);

        _bodyVertexCount = buffer.Count / floatsPerVertex;
        float[] data = [.. buffer];

        GL.BindVertexArray(_bodyVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _bodyVbo);
        GL.BufferData(BufferTarget.ArrayBuffer,
            data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);
        GL.BindVertexArray(0);
    }

    private void AddPlanet(List<float> buffer,
        string name, int year, int month, int day, double hour,
        float r, float g, float b, float size)
    {
        try
        {
            var obs = EphemerisCalculator.GetPlanetPosition(
                name, _vm.SimTime, TimeZoneInfo.Utc.Id, _vm.Longitude, _vm.Latitude);
            AddBodyVertex(buffer, obs.Azimuth, obs.Altitude, r, g, b, size, name);
        }
        catch
        {
            // Skip bodies that can't be computed (e.g., missing orbital elements)
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

        // Store label info for overlay rendering (screen coords computed during paint)
        _labels.Add((PointF.Empty, label, new SKColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255))));
    }

    // ─────────────────────────────────────────────────────────────────────
    // OpenGL rendering
    // ─────────────────────────────────────────────────────────────────────

    private void OnGlPaint(object? sender, PaintEventArgs e)
    {
        if (!_glReady) return;
        _gl.MakeCurrent();

        GL.Viewport(0, 0, _gl.Width, _gl.Height);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.UseProgram(_shaderProgram);

        var mvp = BuildMVP();
        GL.UniformMatrix4(_mvpLoc, false, ref mvp);

        // Draw stars
        GL.BindVertexArray(_starVao);
        GL.DrawArrays(PrimitiveType.Points, 0, _starCount);

        // Draw bodies (on top)
        GL.BindVertexArray(_bodyVao);
        GL.DrawArrays(PrimitiveType.Points, 0, _bodyVertexCount);

        // Draw horizon ring (line loop, uses same shader — color/size not used)
        GL.BindVertexArray(_horizonVao);
        GL.DrawArrays(PrimitiveType.LineLoop, 0, 360);
        GL.BindVertexArray(0);

        _gl.SwapBuffers();

        // Update screen positions for label overlay
        UpdateLabelPositions(mvp);
        _overlayPanel.Invalidate();
    }

    private Matrix4 BuildMVP()
    {
        float aspect = _gl.Width > 0 && _gl.Height > 0
            ? (float)_gl.Width / _gl.Height
            : 1f;

        // Projection: perspective with user-controlled FOV
        var proj = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(_vm.FovDeg), aspect, 0.01f, 10f);

        // View: look from origin in the direction defined by yaw+pitch
        float yawRad   = MathHelper.DegreesToRadians(_vm.Yaw);
        float pitchRad = MathHelper.DegreesToRadians(_vm.Pitch);

        // Target direction in unit-sphere coords
        var forward = new Vector3(
            (float)(Math.Cos(pitchRad) * Math.Sin(yawRad)),
            (float)Math.Sin(pitchRad),
            (float)(Math.Cos(pitchRad) * Math.Cos(yawRad)));

        var view = Matrix4.LookAt(Vector3.Zero, forward, Vector3.UnitY);

        return view * proj;
    }

    private void UpdateLabelPositions(Matrix4 mvp)
    {
        if (_bodyVertexCount == 0 || _gl.Width == 0 || _gl.Height == 0) return;

        // Read body positions back from VBO to compute screen coords
        GL.BindBuffer(BufferTarget.ArrayBuffer, _bodyVbo);
        int floatCount = _bodyVertexCount * 8;
        float[] data = new float[floatCount];
        GL.GetBufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, floatCount * sizeof(float), data);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        for (int i = 0; i < Math.Min(_bodyVertexCount, _labels.Count); i++)
        {
            var worldPos = new Vector4(data[i * 8], data[i * 8 + 1], data[i * 8 + 2], 1f);
            var clipPos  = worldPos * mvp;

            if (clipPos.W <= 0)
            {
                // Behind camera — mark as off-screen
                _labels[i] = (new PointF(-1, -1), _labels[i].Label, _labels[i].Color);
                continue;
            }

            // Perspective divide → NDC
            float ndcX = clipPos.X / clipPos.W;
            float ndcY = clipPos.Y / clipPos.W;

            // NDC → screen pixels
            float screenX = (ndcX + 1f) * 0.5f * _gl.Width;
            float screenY = (1f - ndcY) * 0.5f * _gl.Height; // Y flipped

            _labels[i] = (new PointF(screenX, screenY), _labels[i].Label, _labels[i].Color);
        }
    }

    private void OnGlResize(object? sender, EventArgs e)
    {
        if (!_glReady) return;
        _gl.MakeCurrent();
        GL.Viewport(0, 0, _gl.Width, _gl.Height);
        _gl.Invalidate();
    }

    // ─────────────────────────────────────────────────────────────────────
    // SkiaSharp overlay (labels + HUD)
    // ─────────────────────────────────────────────────────────────────────

    private void OnOverlayPaint(object? sender, PaintEventArgs e)
    {
        int width  = _overlayPanel.Width;
        int height = _overlayPanel.Height;
        if (width <= 0 || height <= 0) return;

        using var bitmap  = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas  = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        using var labelPaint = new SKPaint { IsAntialias = true };
        using var labelFont  = new SKFont(SKTypeface.Default, 12f);
        using var strokePaint = new SKPaint
        {
            IsAntialias = true,
            Color       = SKColors.Black,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = 3f,
        };
        using var strokeFont = new SKFont(SKTypeface.Default, 12f);

        // Body labels
        foreach (var (screen, label, color) in _labels)
        {
            if (screen.X < 0 || screen.X > width || screen.Y < 0 || screen.Y > height)
                continue;

            float tx = screen.X + 8f;
            float ty = screen.Y - 4f;

            labelPaint.Color = color;

            // Black outline for readability
            canvas.DrawText(label, tx, ty, SKTextAlign.Left, strokeFont, strokePaint);
            canvas.DrawText(label, tx, ty, SKTextAlign.Left, labelFont, labelPaint);
        }

        // HUD: date/time, observer location, view direction
        DrawHud(canvas);

        // Convert SKBitmap → System.Drawing.Bitmap and draw on panel
        using var skData  = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream  = skData.AsStream();
        using var gdiBmp  = new Bitmap(stream);
        e.Graphics.DrawImageUnscaled(gdiBmp, 0, 0);
    }

    private void DrawHud(SKCanvas canvas)
    {
        using var hudPaint = new SKPaint { IsAntialias = true, Color = new SKColor(200, 230, 200, 220) };
        using var hudFont  = new SKFont(SKTypeface.Default, 13f);
        using var bgPaint  = new SKPaint { Color = new SKColor(0, 0, 0, 140) };

        string[] lines =
        [
            $"UTC: {_vm.SimTime:yyyy-MM-dd HH:mm:ss}",
            $"Observer: {_vm.Latitude:+0.0°;-0.0°;0.0°} N  {_vm.Longitude:+0.0°;-0.0°;0.0°} E",
            $"View Az: {_vm.Yaw:F0}°  Alt: {_vm.Pitch:F0}°  FOV: {_vm.FovDeg:F0}°",
            "Drag=rotate  Wheel=zoom  Space=play  ←/→=day  F=now",
        ];

        float lineH = 18f;
        float bgH   = lines.Length * lineH + 8f;
        float bgW   = 360f;
        canvas.DrawRect(6, 6, bgW, bgH, bgPaint);

        for (int i = 0; i < lines.Length; i++)
            canvas.DrawText(lines[i], 12f, 20f + i * lineH, SKTextAlign.Left, hudFont, hudPaint);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Coordinate helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts azimuth/altitude (degrees) to a unit-sphere position vector.
    /// Coordinate frame: +Y = zenith, +Z = north, +X = east.
    /// </summary>
    private static (float X, float Y, float Z) AzAltToUnitSphere(double azimuthDeg, double altitudeDeg)
    {
        double azRad  = double.DegreesToRadians(azimuthDeg);
        double altRad = double.DegreesToRadians(altitudeDeg);
        float x = (float)(Math.Cos(altRad) * Math.Sin(azRad));
        float y = (float)Math.Sin(altRad);
        float z = (float)(Math.Cos(altRad) * Math.Cos(azRad));
        return (x, y, z);
    }

    /// <summary>
    /// Returns an approximate RGB color for a star based on its spectral type.
    /// </summary>
    private static (float R, float G, float B) SpectralColor(string spectralType, float magnitude)
    {
        char spectralClass = spectralType.Length > 0 ? spectralType[0] : 'G';
        float dim = Math.Clamp(1f - (magnitude - 1f) / 8f, 0.3f, 1f);

        return spectralClass switch
        {
            'O' => (0.6f * dim, 0.7f * dim, 1.0f * dim),  // blue
            'B' => (0.7f * dim, 0.8f * dim, 1.0f * dim),  // blue-white
            'A' => (0.9f * dim, 0.9f * dim, 1.0f * dim),  // white-blue
            'F' => (1.0f * dim, 1.0f * dim, 0.9f * dim),  // white-yellow
            'G' => (1.0f * dim, 0.95f * dim, 0.7f * dim), // yellow (Sun-like)
            'K' => (1.0f * dim, 0.7f * dim, 0.4f * dim),  // orange
            'M' => (1.0f * dim, 0.4f * dim, 0.2f * dim),  // red
            _   => (0.9f * dim, 0.9f * dim, 0.9f * dim),  // white default
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // Input handling
    // ─────────────────────────────────────────────────────────────────────

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging   = true;
            _lastMouse  = e.Location;
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            _dragging = false;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;

        float dx = e.X - _lastMouse.X;
        float dy = e.Y - _lastMouse.Y;

        _vm.Yaw   = (_vm.Yaw   + dx * 0.4f + 360f) % 360f;
        _vm.Pitch = Math.Clamp(_vm.Pitch - dy * 0.3f, -10f, 90f);

        _lastMouse = e.Location;
        _gl.Invalidate();
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        _vm.FovDeg = Math.Clamp(_vm.FovDeg - e.Delta * 0.02f, 10f, 170f);
        _gl.Invalidate();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Space:
                _vm.PlayPauseCommand.Execute(null);
                break;
            case Keys.Left:
                _vm.StepBackCommand.Execute(null);
                break;
            case Keys.Right:
                _vm.StepForwardCommand.Execute(null);
                break;
            case Keys.F:
                _vm.ResetToNowCommand.Execute(null);
                break;
            case Keys.Up:
                _vm.FovDeg = Math.Clamp(_vm.FovDeg - 5f, 10f, 170f);
                _gl.Invalidate();
                break;
            case Keys.Down:
                _vm.FovDeg = Math.Clamp(_vm.FovDeg + 5f, 10f, 170f);
                _gl.Invalidate();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // ViewModel property-change synchronisation
    // ─────────────────────────────────────────────────────────────────────

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (InvokeRequired) { Invoke(() => OnViewModelPropertyChanged(sender, e)); return; }

        _syncingFromVm = true;
        try
        {
            switch (e.PropertyName)
            {
                case nameof(SkyViewModel.SimTime):
                    _datePicker.Value = _vm.SimTime.ToLocalTime();
                    InvalidateScene();
                    break;
                case nameof(SkyViewModel.Longitude):
                    _lonPicker.Value = (decimal)_vm.Longitude;
                    InvalidateScene();
                    break;
                case nameof(SkyViewModel.Latitude):
                    _latPicker.Value = (decimal)_vm.Latitude;
                    InvalidateScene();
                    break;
                case nameof(SkyViewModel.Playing):
                    _playBtn.Text = _vm.Playing ? "⏸ Pause" : "▶ Play";
                    if (_vm.Playing) _animTimer.Start(); else _animTimer.Stop();
                    break;
                case nameof(SkyViewModel.Yaw):
                case nameof(SkyViewModel.Pitch):
                case nameof(SkyViewModel.FovDeg):
                    InvalidateScene();
                    break;
            }
        }
        finally
        {
            _syncingFromVm = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Cleanup
    // ─────────────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animTimer.Stop();
            _animTimer.Dispose();

            if (_glReady)
            {
                _gl.MakeCurrent();
                GL.DeleteProgram(_shaderProgram);
                GL.DeleteVertexArray(_starVao);
                GL.DeleteBuffer(_starVbo);
                GL.DeleteVertexArray(_bodyVao);
                GL.DeleteBuffer(_bodyVbo);
                GL.DeleteVertexArray(_horizonVao);
                GL.DeleteBuffer(_horizonVbo);
                _glReady = false;
            }
        }

        base.Dispose(disposing);
    }
}
