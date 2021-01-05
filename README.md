# Avalonia.Cubism

Live2D control for Avalonia.
`yatli/Avalonia.Cubism <-[fork]- vignette-project/CubismFrameworkCS <-[fork]- Nkyoku/CubismFrameworkCS`

#### Porting to Avalonia

1. Replace osu with Avalonia.OpenGL.
2. Wraps up `Avalonia.Cubism.Live2DControl` in .NET Standard 2.1
3. Depends on `Avalonia.OpenGL`, a few extensions to GlInterface are added.
4. Supports loading Live2D assets from file or avares.

# CubismFrameworkCS
Live2D Cubism 3 SDK NativeをUnityを使わず勝手にC#移植

#### Notes
This fork ports this library to .NET Core 3.1 and makes a few adjustments listed below:

1. `ICubismClippingMask`, `ICubismRenderer`, `ICubismTexture` are now interfaces.
2. `ICubismRenderer.BlendModeType` is now simply `BlendModeType`.
3. `MathNet.Numerics.LinearAlgebra.Single.Matrix` is now `System.Numerics.Matrix4x4`.
4. Supports Live2D Cubism 4 Mask Inversion

