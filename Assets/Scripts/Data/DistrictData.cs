using UnityEngine;

namespace HeroDispatch
{
    // 区域数据（独立文件：类名必须与文件名一致）
    [CreateAssetMenu(fileName = "NewDistrict", menuName = "HeroDispatch/DistrictData")]
    public class DistrictData : ScriptableObject
    {
        public string districtId;
        public string districtName;
        public string emoji;
        public float safety = 80f;
        public Vector2 mapPos;   // 归一化 0-1
        public Vector2 mapSize = new Vector2(120, 80);
        public Color districtColor = Color.white;
    }
}
