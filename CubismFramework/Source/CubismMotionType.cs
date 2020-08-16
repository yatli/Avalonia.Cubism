namespace CubismFramework
{
    /// <summary>
    /// モーションの種別
    /// </summary>
    public enum MotionType
    {
        /// <summary>
        /// ベースモーション。
        /// 前フレームで行われたパラメータの変更に引き続きパラメータを更新する。
        /// </summary>
        Base,

        /// <summary>
        /// 表情モーション。
        /// エフェクトが及ぼしたパラメータへの変更は次のフレームには引き継がれない。
        /// </summary>
        Expression,

        /// <summary>
        /// エフェクトとなるモーション。
        /// エフェクトが及ぼしたパラメータへの変更は次のフレームには引き継がれない。
        /// </summary>
        Effect
    }
}