using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using Ecliptica.Core.Models;
using Ecliptica.Core.Interfaces;

namespace Ecliptica.Renderer;

public class OpenGLRenderContext : IDisposable
{
    private GL? _gl;
    private uint _program;
    private uint _vao;
    private uint _vbo;

    private readonly string _vertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec2 aPos;
        uniform mat4 uProjection;
        uniform mat4 uView;
        uniform vec3 uOffset;
        uniform float uScale;

        void main()
        {
            vec4 worldPos = vec4(aPos.x * uScale + uOffset.x, aPos.y * uScale + uOffset.y, uOffset.z, 1.0);
            gl_Position = uProjection * uView * worldPos;
        }
    ";

    private readonly string _fragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        uniform vec4 uColor;

        void main()
        {
            FragColor = uColor;
        }
    ";

    public unsafe void Initialize(GL gl)
    {
        _gl = gl;

        // Compile shaders
        using var loader = new ShaderLoader(_gl);
        _program = loader.CompileShader(_vertexShaderSource, _fragmentShaderSource);

        // Simple circle geometry (representing spheres in 2D space coordinates)
        int segments = 32;
        var vertices = new List<float>();
        for (int i = 0; i <= segments; i++)
        {
            double angle = 2.0 * Math.PI * i / segments;
            vertices.Add((float)Math.Cos(angle));
            vertices.Add((float)Math.Sin(angle));
        }

        // Allocate buffers
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        
        unsafe
        {
            fixed (float* v = vertices.ToArray())
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Count * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }

        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), null);
        _gl.EnableVertexAttribArray(0);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }

    public void Render(SimulationSnapshot snapshot, int width, int height, float cameraPanX, float cameraPanY, float cameraZoom)
    {
        if (_gl == null) return;

        _gl.ClearColor(Color.FromArgb(5, 6, 15)); // DeepSpace background sync
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _gl.UseProgram(_program);
        _gl.BindVertexArray(_vao);

        // Projection matrix
        var projection = Matrix4x4.CreateOrthographicOffCenter(-width / 2.0f, width / 2.0f, -height / 2.0f, height / 2.0f, -1.0f, 1.0f);
        SetMatrix4x4("uProjection", projection);

        // Camera view matrix
        var view = Matrix4x4.CreateTranslation(new Vector3(cameraPanX, cameraPanY, 0.0f)) * Matrix4x4.CreateScale(cameraZoom);
        SetMatrix4x4("uView", view);

        // Draw each body as a simple circle sphere
        foreach (var body in snapshot.Bodies)
        {
            // Position offset conversion
            int offsetLoc = _gl.GetUniformLocation(_program, "uOffset");
            _gl.Uniform3(offsetLoc, (float)body.Position.X, (float)body.Position.Y, (float)body.Position.Z);
            
            // Sphere radius scaling representation (clamped for visual display)
            float radius = (float)body.Radius;
            if (radius < 5.0f) radius = 5.0f; // guarantee visibility
            
            int scaleLoc = _gl.GetUniformLocation(_program, "uScale");
            _gl.Uniform1(scaleLoc, radius);

            // Color representation
            var color = new Vector4(0.8f, 0.8f, 0.9f, 1.0f); // Default white/blue
            if (body.Name.Contains("Sun") || body.Name.Contains("Star"))
            {
                color = new Vector4(1.0f, 0.9f, 0.3f, 1.0f); // Yellow star
            }
            else if (body.Name.Contains("Jet"))
            {
                color = new Vector4(0.3f, 0.6f, 1.0f, 0.8f); // Blue jet
            }
            else if (body.Name.Contains("Disk"))
            {
                color = new Vector4(1.0f, 0.5f, 0.1f, 0.7f); // Orange disk
            }
            
            int colorLoc = _gl.GetUniformLocation(_program, "uColor");
            _gl.Uniform4(colorLoc, color.X, color.Y, color.Z, color.W);

            _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 33);
        }

        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
    }

    private void SetMatrix4x4(string name, Matrix4x4 matrix)
    {
        int location = _gl!.GetUniformLocation(_program, name);
        unsafe
        {
            _gl.UniformMatrix4(location, 1, false, (float*)&matrix);
        }
    }

    public void Dispose()
    {
        if (_gl != null)
        {
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteProgram(_program);
        }
    }
}
