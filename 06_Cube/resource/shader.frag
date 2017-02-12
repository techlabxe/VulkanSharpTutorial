#version 450

layout(location=0) in vec4 InputCol;

layout(location=0) out vec4 OutputCol;

void main()
{
    OutputCol = InputCol;
}
