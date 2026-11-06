using UnityEngine;

namespace Kyrnel
{
    public static class PositionUtils
    {
        // 自定义精度阈值（1e-6），比 float.Epsilon 更符合实际计算误差
        private const float PrecisionEpsilon = 1e-6f;

        /// <summary>
        /// 计算当前物体应处于的正确位置，使其与目标位置保持至少 radius 距离。
        /// 如果距离过近，返回一个新位置；否则返回原位置。
        /// </summary>
        /// <param name="currentPos">当前物体位置（会被修正）</param>
        /// <param name="targetPos">目标中心位置（需要避开的位置）</param>
        /// <param name="radius">最小允许距离</param>
        /// <returns>修正后的世界坐标</returns>
        public static Vector3 GetSeparatedPosition(Vector3 currentPos, Vector3 targetPos, float radius)
        {
            // 注意：这里直接使用传入参数的副本进行运算，不影响外部变量
            Vector3 posXZ = new Vector3(currentPos.x, 0f, currentPos.z);
            Vector3 targetXZ = new Vector3(targetPos.x, 0f, targetPos.z);

            float distance = Vector3.Distance(posXZ, targetXZ);

            // 情况1：距离足够远，无需修正
            if (distance >= radius)
            {
                return currentPos; // 保持原样
            }

            // 情况2：几乎完全重叠（或极端接近），使用随机位置逃逸
            if (distance <= PrecisionEpsilon)
            {
                Vector3 randomPos = GetRandomPositionNearby(targetPos, radius, radius);
                // 保留原始 Y 轴高度，防止角色掉到地上
                randomPos.y = currentPos.y;
                return randomPos;
            }

            // 情况3：距离在 (0, radius) 之间，沿径向推离到边界
            Vector3 direction = (posXZ - targetXZ).normalized;
            Vector3 newPos = targetPos + direction * radius;

            // 保留原始 Y 轴高度
            newPos.y = currentPos.y;
            return newPos;
        }

        /// <summary>
        /// 在指定位置周围生成一个随机偏移点（正方形区域随机）
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="maxRadius">最大偏移半径</param>
        /// <param name="minRadius">最小偏移半径</param>
        /// <returns>偏移后的世界坐标（Y 轴不变）</returns>
        public static Vector3 GetRandomPositionNearby(Vector3 center, float maxRadius, float minRadius)
        {
            // 修复：使用 Random.value 替代未定义的 Chance() 方法
            float signX = Random.value < 0.5f ? 1f : -1f;
            float signZ = Random.value < 0.5f ? 1f : -1f;

            float offsetX = Random.Range(minRadius, maxRadius) * signX;
            float offsetZ = Random.Range(minRadius, maxRadius) * signZ;

            return new Vector3(center.x + offsetX, center.y, center.z + offsetZ);
        }
    }
}