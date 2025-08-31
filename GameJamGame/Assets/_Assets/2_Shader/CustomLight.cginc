float Bayer4x4_float(float2 UV)
{
    // pixel coords
    uint2 p = (uint2)(UV * _ScreenParams.xy);
    uint x = p.x & 3u;
    uint y = p.y & 3u;
    uint idx = (y << 2) | x;

    const float b[16] = {
        0.0/16.0, 8.0/16.0, 2.0/16.0,10.0/16.0,
       12.0/16.0, 4.0/16.0,14.0/16.0, 6.0/16.0,
        3.0/16.0,11.0/16.0, 1.0/16.0, 9.0/16.0,
       15.0/16.0, 7.0/16.0,13.0/16.0, 5.0/16.0
    };
    return b[idx]; // 0..1
}
