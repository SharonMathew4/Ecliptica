using System;
using System.IO;
using Silk.NET.OpenGL;

namespace Ecliptica.Renderer;

public class ShaderLoader : IDisposable
{
    private readonly GL _gl;

    public ShaderLoader(GL gl)
    {
        _gl = gl;
    }

    public uint CompileShader(string vertexShaderSource, string fragmentShaderSource)
    {
        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);
        CheckShaderCompileError(vertexShader);

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentShaderSource);
        _gl.CompileShader(fragmentShader);
        CheckShaderCompileError(fragmentShader);

        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vertexShader);
        _gl.AttachShader(program, fragmentShader);
        _gl.LinkProgram(program);
        CheckProgramLinkError(program);

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        return program;
    }

    private void CheckShaderCompileError(uint shader)
    {
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0)
        {
            string log = _gl.GetShaderInfoLog(shader);
            throw new Exception($"Shader compilation failed: {log}");
        }
    }

    private void CheckProgramLinkError(uint program)
    {
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0)
        {
            string log = _gl.GetProgramInfoLog(program);
            throw new Exception($"Shader linking failed: {log}");
        }
    }

    public void Dispose()
    {
    }
}
