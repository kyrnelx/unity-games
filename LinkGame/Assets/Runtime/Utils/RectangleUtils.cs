using UnityEngine;

namespace Kyrnel
{
    public static class RetangleUtils
    {
        private static (float minX, float maxX, float minY, float maxY) GetRectangleBounds(
            Vector2 position, float pointSize)
        {
            var halfPointSize = pointSize / 2f;
            return (position.x - halfPointSize, position.x + halfPointSize, position.y - halfPointSize,
                    position.y + halfPointSize);
        }

        /// <summary>
        ///  判断点是否在矩形内
        /// </summary>
        /// <param name="rPointA"></param>
        /// <param name="fPointSize"></param>
        /// <param name="rPointB"></param>
        /// <returns></returns>
        public static bool CheckIntersect(Vector2 rPointA, float fPointSize, Vector2 rPointB)
        {
            var (minX, maxX, minY, maxY) = GetRectangleBounds(rPointA, fPointSize);
            return minX <= rPointB.x && rPointB.x <= maxX && minY <= rPointB.y && rPointB.y <= maxY;
        }

        /// <summary>
        ///  判断两个矩形是否相交
        /// </summary>
        /// <param name="rRectanglePositionA"></param>
        /// <param name="rRectanglePositionB"></param>
        /// <param name="fPointSize"></param>
        /// <returns></returns>
        public static bool CheckIntersect(Vector2 rRectanglePositionA, Vector2 rRectanglePositionB, float fPointSize)
        {
            var (aLeft, aRight, aBottom, aTop) = GetRectangleBounds(rRectanglePositionA, fPointSize);
            var (bLeft, bRight, bBottom, bTop) = GetRectangleBounds(rRectanglePositionB, fPointSize);

            if (aRight < bLeft || aLeft > bRight)
                return false;

            if (aBottom > bTop || aTop < bBottom)
                return false;

            return true;
        }
    }
}