# CubismFrameworkCS
Live2D Cubism 3 SDK NativeをUnityを使わず勝手にC#移植

#### Notes
This fork ports this library to .NET Core 3.1 and makes a few adjustments listed below:

1. `ICubismClippingMask`, `ICubismRenderer`, `ICubismTexture` are now interfaces.
2. `ICubismRenderer.BlendModeType` is now simply `BlendModeType`.
3. `MathNet.Numerics.LinearAlgebra.Single.Matrix` is now `System.Numerics.Matrix4`.
4. Supports Live2D Cubism 4 Mask Inversion
