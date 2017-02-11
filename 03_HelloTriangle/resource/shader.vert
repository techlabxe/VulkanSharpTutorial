#version 450

layout(location=0) in vec3 InPos;
layout(location=1) in vec4 InCol;

layout(location=0) out vec4 OutCol;


out gl_PerVertex
{
  vec4 gl_Position;
};

void main()
{
    gl_Position = vec4(InPos, 1.0);
    OutCol = InCol;
}
