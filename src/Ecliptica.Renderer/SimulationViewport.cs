using System;
using System.Drawing;
using System.Windows.Forms;
using Silk.NET.OpenGL;
using OpenTK.WinForms;
using Ecliptica.Core.Models;

namespace Ecliptica.Renderer;

public class SimulationViewport : GLControl
{
    private GL? _gl;
    private OpenGLRenderContext? _renderContext;
    private SimulationSnapshot? _currentSnapshot;

    // Viewport camera parameters
    private float _cameraPanX = 0.0f;
    private float _cameraPanY = 0.0f;
    private float _cameraZoom = 1.0f;

    // Drag-pan variables
    private bool _isDragging = false;
    private Point _lastMousePos;

    public SimulationViewport()
    {
        // Hook mouse events for pan/zoom controls
        MouseWheel += OnViewportMouseWheel;
        MouseDown += OnViewportMouseDown;
        MouseUp += OnViewportMouseUp;
        MouseMove += OnViewportMouseMove;

        Load += OnViewportLoad;
        Resize += OnViewportResize;
        Paint += OnViewportPaint;
    }

    public void UpdateSnapshot(SimulationSnapshot snapshot)
    {
        _currentSnapshot = snapshot;
        Invalidate(); // Trigger Paint redraw natively inside GLControl
    }

    private void OnViewportLoad(object? sender, EventArgs e)
    {
        MakeCurrent(); // Bind OpenGL context to this control thread

        // Load Silk.NET API wrappers matching OpenTK context bindings using internal GLFW lookup context
        _gl = GL.GetApi(name => 
        {
            var ptr = OpenTK.Windowing.GraphicsLibraryFramework.GLFW.GetProcAddress(name);
            if (ptr == IntPtr.Zero)
            {
                // Fallback to OpenTK context interface loader via GLFW native delegate loader
                ptr = OpenTK.Windowing.GraphicsLibraryFramework.GLFW.GetProcAddress(name);
            }
            return ptr;
        });
        
        _renderContext = new OpenGLRenderContext();
        _renderContext.Initialize(_gl);
    }

    private void OnViewportResize(object? sender, EventArgs e)
    {
        MakeCurrent();
        if (_gl != null)
        {
            _gl.Viewport(0, 0, (uint)Width, (uint)Height);
        }
        Invalidate();
    }

    private void OnViewportPaint(object? sender, PaintEventArgs e)
    {
        MakeCurrent();
        if (_gl != null && _renderContext != null && _currentSnapshot != null)
        {
            _renderContext.Render(_currentSnapshot, Width, Height, _cameraPanX, _cameraPanY, _cameraZoom);
            SwapBuffers();
        }
        else
        {
            // Fallback clear using OpenGL to prevent graphics context corruption / hall-of-mirrors effect
            if (_gl != null)
            {
                _gl.ClearColor(5f / 255f, 6f / 255f, 15f / 255f, 1f);
                _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                SwapBuffers();
            }
        }
    }

    private void OnViewportMouseWheel(object? sender, MouseEventArgs e)
    {
        float scaleFactor = e.Delta > 0 ? 1.1f : 0.9f;
        _cameraZoom *= scaleFactor;

        if (_cameraZoom < 1e-5f) _cameraZoom = 1e-5f;
        if (_cameraZoom > 1e5f) _cameraZoom = 1e5f;

        Invalidate();
    }

    private void OnViewportMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
        {
            _isDragging = true;
            _lastMousePos = e.Location;
        }
    }

    private void OnViewportMouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void OnViewportMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            float dx = e.X - _lastMousePos.X;
            float dy = _lastMousePos.Y - e.Y;
            _cameraPanX += dx / _cameraZoom;
            _cameraPanY += dy / _cameraZoom;
            _lastMousePos = e.Location;
            Invalidate();
        }
    }

    public Vector3d ScreenToWorld(int screenX, int screenY)
    {
        double dx = screenX - Width / 2.0;
        double dy = Height / 2.0 - screenY;
        double worldX = dx / _cameraZoom - _cameraPanX;
        double worldY = dy / _cameraZoom - _cameraPanY;
        return new Vector3d(worldX, worldY, 0.0);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderContext?.Dispose();
        }
        base.Dispose(disposing);
    }
}
