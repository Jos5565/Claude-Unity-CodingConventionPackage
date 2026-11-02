using System;

namespace UnityConv.DI
{
    /// <summary>
    /// SceneInstaller가 씬 컨테이너의 값을 넣을 필드 (Manager만 사용, UI는 금지)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
