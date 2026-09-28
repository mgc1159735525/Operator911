#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace HeroDispatch
{
    // 一键创建所有默认数据资产（20英雄/24事件/6区域）并绑定立绘与技能图标
    public static class DefaultDataInitializer
    {
        const string HeroDir = "Assets/ScriptableObjects/Heroes";
        const string EventDir = "Assets/ScriptableObjects/Events";
        const string DistrictDir = "Assets/ScriptableObjects/Districts";
        const string PortraitDir = "Assets/Art/Heroes";
        const string SkillIconDir = "Assets/Art/Skills";

        [MenuItem("HeroDispatch/创建所有默认数据")]
        public static void CreateAll()
        {
            EnsureFolder(HeroDir);
            EnsureFolder(EventDir);
            EnsureFolder(DistrictDir);
            EnsureSprites(PortraitDir);
            EnsureSprites(SkillIconDir);

            CreateHeroes();
            CreateEvents();
            CreateDistricts();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DefaultDataInitializer] 20英雄/24事件/6区域 全部创建完成，立绘与技能图标已绑定！");
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                // 磁盘已存在的目录直接跳过（避免 CreateFolder 重名产生 "xxx 1"）
                if (System.IO.Directory.Exists(next) && AssetDatabase.IsValidFolder(next)) { cur = next; continue; }
                if (!System.IO.Directory.Exists(next) && !AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        // 把目录下所有 jpg/png 设置为 Sprite（2D and UI）
        static void EnsureSprites(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.GetFiles(dir, "*.jpg").Concat(Directory.GetFiles(dir, "*.png")))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
                if (importer == null) continue;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
        }

        static Sprite LoadSprite(string dir, string file)
        {
            string path = dir + "/" + file;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ==================== 英雄 ====================
        static void CreateHeroes()
        {
            foreach (var r in HeroTable.Heroes)
            {
                string path = HeroDir + "/" + Cap(r.id) + "_" + r.codename + ".asset";
                var h = AssetDatabase.LoadAssetAtPath<HeroData>(path);
                if (h == null)
                {
                    h = ScriptableObject.CreateInstance<HeroData>();
                    AssetDatabase.CreateAsset(h, path);
                }

                h.heroId = r.id;
                h.codename = r.codename;
                h.displayName = r.name;
                h.archetype = r.arch;
                h.emoji = r.emoji;
                ColorUtility.TryParseHtmlString(r.color, out h.color);
                h.level = 1;
                h.salary = r.salary;
                h.powerName = r.power;
                h.str = r.str; h.spd = r.spd; h.intl = r.intl; h.end = r.end; h.per = r.per;
                h.quirkName = r.quirk;
                h.quirkDesc = r.quirkDesc;
                h.backstory = r.bio;

                h.portraitNormal = LoadSprite(PortraitDir, "hero_" + r.id + "_normal.jpg");
                h.portraitInjured = LoadSprite(PortraitDir, "hero_" + r.id + "_injured.jpg");

                var types = new[] { SkillType.Passive, SkillType.Active, SkillType.Ultimate };
                var suffixes = new[] { "passive", "active", "ultimate" };
                h.skills = new SkillData[3];
                for (int i = 0; i < 3; i++)
                {
                    h.skills[i] = new SkillData
                    {
                        name = r.skills[i].n,
                        type = types[i],
                        desc = r.skills[i].d,
                        icon = LoadSprite(SkillIconDir, "skill_" + r.id + "_" + suffixes[i] + ".jpg")
                    };
                }

                EditorUtility.SetDirty(h);
            }
        }

        // ==================== 事件 ====================
        static void CreateEvents()
        {
            int i = 0;
            foreach (var e in EventTable.Events)
            {
                string path = EventDir + "/" + (i++).ToString("00") + "_" + e.name + ".asset";
                var t = AssetDatabase.LoadAssetAtPath<EventTemplate>(path);
                if (t == null)
                {
                    t = ScriptableObject.CreateInstance<EventTemplate>();
                    AssetDatabase.CreateAsset(t, path);
                }
                t.eventType = e.type;
                t.eventName = e.name;
                t.icon = e.icon;
                t.description = e.desc;
                t.fogDescription = e.fogDesc;
                t.needs = e.needs;
                t.baseGrade = e.baseGrade;
                EditorUtility.SetDirty(t);
            }
        }

        // ==================== 区域 ====================
        static void CreateDistricts()
        {
            foreach (var d in EventTable.Districts)
            {
                string path = DistrictDir + "/" + d.id + "_" + d.name + ".asset";
                var a = AssetDatabase.LoadAssetAtPath<DistrictData>(path);
                if (a == null)
                {
                    a = ScriptableObject.CreateInstance<DistrictData>();
                    AssetDatabase.CreateAsset(a, path);
                }
                a.districtId = d.id;
                a.districtName = d.name;
                a.emoji = d.emoji;
                a.safety = 80f;
                a.mapPos = EventTable.ToNormalized(d.pos);
                a.mapSize = new Vector2(d.size.x / 600f, d.size.y / 430f);
                ColorUtility.TryParseHtmlString(d.color, out a.districtColor);
                EditorUtility.SetDirty(a);
            }
        }

        static string Cap(string s) => char.ToUpper(s[0]) + s.Substring(1);
    }
}
#endif
