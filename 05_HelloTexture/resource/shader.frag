#version 450

layout(location=0) in vec2 InputUV0;

layout(location=0) out vec4 OutputCol;

layout(binding=1) uniform sampler2D sampleTex;

void main()
{
    OutputCol = texture( sampleTex, InputUV0 );
}
