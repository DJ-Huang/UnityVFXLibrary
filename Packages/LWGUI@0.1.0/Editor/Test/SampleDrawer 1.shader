Shader "Hidden"
{
    Properties
    {
        [Title(_, Main Samples)]

        [Main(GroupName)]
        _group ("Group", float) = 0
        [Sub(GroupName)] _float ("Float", float) = 0


        [Main(Group1, _KEYWORD, on)]
        _group1 ("Group - Default Open", float) = 1
        [Sub(Group1)] _float1 ("Sub Float", float) = 0
        [Sub(Group1)][HDR] _color1 ("Sub HDR Color", color) = (0.7, 0.7, 1, 1)

        [Title(Group1, Conditional Display Samples       Enum)]
        [KWEnum(Group1, Name 1, _KEY1, Name 2, _KEY2, Name 3, _KEY3)] _enum ("Sub Enum", float) = 0

        // Display when the keyword ("group name + keyword") is activated
        [Sub(Group1_KEY1)] _key1_Float1 ("Key1 Float", float) = 0
        [Sub(Group1_KEY2)] _key2_Float2 ("Key2 Float", float) = 0
        [Sub(Group1_KEY3)] _key3_Float3 ("Key3 Float", float) = 0
        [SubPowerSlider(Group1_KEY3, 10)] _key3_Float4_PowerSlider ("Key3 Power Slider", Range(0, 1)) = 0

        [Title(Group1, Conditional Display Samples       Toggle)]
        [SubToggle(Group1, _TOGGLE_KEYWORD)] _toggle ("Sub Toggle", float) = 0
        [Tex(Group1_TOGGLE_KEYWORD)][Normal] _normal ("Normal Keyword", 2D) = "bump" { }
        [Sub(Group1_TOGGLE_KEYWORD)] _float2 ("Float Keyword", float) = 0


        [Main(Group2, _, off, off)]
        _group2 ("Group - Without Toggle", float) = 0
        [Sub(Group2)] _float3 ("Float 2", float) = 0


        [Space]
        [Title(_, Tex and Color Samples)]

        [Tex(_, _color)] _tex ("Tex with Color", 2D) = "white" { }
        [HideInInspector] _color (" ", Color) = (1, 0, 0, 1)

        // Display up to 4 colors in a single line (Unity 2019.2+)
        [Color(_, _mColor1, _mColor2, _mColor3)]
        _mColor ("Multi Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _mColor1 (" ", Color) = (1, 0, 0, 1)
        [HideInInspector] _mColor2 (" ", Color) = (0, 1, 0, 1)
        [HideInInspector] [HDR] _mColor3 (" ", Color) = (0, 0, 1, 1)


        [Space]
        [Title(_, Ramp Samples)]
        [Ramp] _Ramp ("Ramp Map", 2D) = "white" { }


        [Space]
        [Title(_, MinMaxSlider Samples)]

        [MinMaxSlider(_, _rangeStart, _rangeEnd)] _minMaxSlider("Min Max Slider (0 - 1)", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _rangeStart("Range Start", Range(0.0, 0.5)) = 0.0
        [HideInInspector] _rangeEnd("Range End", Range(0.5, 1.0)) = 1.0


        [Space]
        [Title(_, Channel Samples)]
        [Channel(_)]_textureChannelMask("Texture Channel Mask (Default G)", Vector) = (0,1,0,0)
    }
    
    HLSLINCLUDE
    
    
    
    ENDHLSL
    
    SubShader
    {
        
        Pass
        {
                

        }
    }
    CustomEditor "LWGUI.LWGUI"
}
