#version 450

layout(location=0) in vec3 InPos;
layout(location=1) in vec2 InUV0;

layout(location=0) out vec2 OutUV0;

layout(binding=0) uniform Transform
{
  mat4 world;
  mat4 view;
  mat4 proj;
} transform;

out gl_PerVertex
{
  vec4 gl_Position;
};

void main()
{
    mat4 pv = transform.proj * transform.view;
    gl_Position = pv * transform.world * vec4(InPos, 1.0);
	OutUV0 = InUV0;
}
