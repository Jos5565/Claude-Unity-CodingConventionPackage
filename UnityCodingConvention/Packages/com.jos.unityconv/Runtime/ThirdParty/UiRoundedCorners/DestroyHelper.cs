using UnityEngine;

namespace Nobi.UiRoundedCorners
{
	public static class DestroyHelper
	{
		public static void Destroy(Object @object)
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
			{
				Object.Destroy(@object);
			}
			else
			{
				Object.DestroyImmediate(@object);
			}
#else
			Object.Destroy(@object);
#endif
		}
	}
}
