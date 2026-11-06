using UnityEngine;
using System.Collections.Generic;

namespace Kyrnel
{
    public static class RadialLayoutCalculator
    {
        /// <summary>
        /// 生成关于中心方向对称分布的扇形角度数组（弧度/角度均可，这里返回度数）
        /// </summary>
        /// <param name="count">元素数量</param>
        /// <param name="angleInterval">相邻元素之间的夹角（度）</param>
        /// <param name="centerDirection">对称中心的世界朝向角度（度），默认为0（即正Z轴或正右方）</param>
        /// <returns>每个元素的绝对世界角度列表</returns>
        public static float[] GetSymmetricAngles(int count, float angleInterval, float centerDirection = 0f)
        {
            if (count <= 0) return System.Array.Empty<float>();

            float   totalAngle = (count - 1) * angleInterval;
            float   startAngle = totalAngle  / 2f; // 最右侧起始
            float[] angles     = new float[count];

            for (int i = 0; i < count; i++)
            {
                // 计算相对于中心方向的偏移，并加上基准方向
                float relativeAngle = startAngle - i * angleInterval;
                angles[i] = centerDirection + relativeAngle;
            }
            return angles;
        }

        /// <summary>
        /// 将元素均匀分布在水平弧形上（基于Y轴旋转）
        /// </summary>
        public static void ApplyArcFormation(IList<Transform> objects, float centerYAngle, float angleInterval, float radius)
        {
            float[] angles = GetSymmetricAngles(objects.Count, angleInterval, centerYAngle);
        
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] == null) continue;
            
                // 将角度转为方向向量（假设朝向 -Z 轴为 0° 基准，适用于 3D 人物）
                Quaternion rotation  = Quaternion.Euler(0f, angles[i], 0f);
                Vector3    direction = rotation * Vector3.back; // 或 Vector3.forward，取决于你的模型朝向
            
                // 设置位置（围绕中心点(0,0,0)分布，可额外传入中心点偏移）
                objects[i].position = direction * radius;
            
                // 如果希望物体始终看向圆心或面朝外，在这里额外设置 rotation
                objects[i].rotation = rotation; 
            }
        }
    }
}