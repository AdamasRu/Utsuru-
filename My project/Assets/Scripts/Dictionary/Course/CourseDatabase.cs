using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utsuru.Course
{
    /// <summary>
    /// Database penyimpan dan penyedia kurikulum Irodori Starter A1.
    /// Memuat data dari Resources/CourseData/irodori_a1_course.json.
    /// Diletakkan di GameObject Managers di scene.
    /// </summary>
    public class CourseDatabase : MonoBehaviour
    {
        public static CourseDatabase Instance { get; private set; }

        private const string CourseDataResourcePath = "CourseData/irodori_a1_course";
        private const string IllustrationResourceFolder = "CourseIllustrations/";

        private CourseDataRoot dataRoot;
        private readonly List<CourseLesson> allLessons = new List<CourseLesson>();
        private readonly Dictionary<string, CourseTopic> topicLookup = new Dictionary<string, CourseTopic>();
        private readonly Dictionary<string, CourseLesson> lessonLookup = new Dictionary<string, CourseLesson>();
        private readonly Dictionary<string, CourseActivity> activityLookup = new Dictionary<string, CourseActivity>();
        private readonly Dictionary<string, Texture2D> illustrationCache = new Dictionary<string, Texture2D>();

        private int totalActivityCount = 0;
        private bool isLoaded = false;

        public CourseDataRoot Data
        {
            get
            {
                EnsureLoaded();
                return dataRoot;
            }
        }

        public IReadOnlyList<CourseTopic> Topics
        {
            get
            {
                EnsureLoaded();
                return dataRoot != null ? dataRoot.topics : new List<CourseTopic>();
            }
        }

        public IReadOnlyList<CourseLesson> AllLessons
        {
            get
            {
                EnsureLoaded();
                return allLessons;
            }
        }

        public int TotalActivityCount
        {
            get
            {
                EnsureLoaded();
                return totalActivityCount;
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureLoaded();
        }

        public void EnsureLoaded()
        {
            if (isLoaded) return;

            TextAsset jsonFile = Resources.Load<TextAsset>(CourseDataResourcePath);
            if (jsonFile == null)
            {
                Debug.LogWarning($"[CourseDatabase] File kurikulum tidak ditemukan di Resources/{CourseDataResourcePath}.json!");
                return;
            }

            try
            {
                dataRoot = JsonUtility.FromJson<CourseDataRoot>(jsonFile.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CourseDatabase] Gagal mengurai JSON kurikulum: {ex.Message}");
                return;
            }

            if (dataRoot == null || dataRoot.topics == null)
            {
                Debug.LogWarning("[CourseDatabase] Data kurikulum kosong setelah parsing.");
                return;
            }

            allLessons.Clear();
            topicLookup.Clear();
            lessonLookup.Clear();
            activityLookup.Clear();
            totalActivityCount = 0;

            foreach (var topic in dataRoot.topics)
            {
                if (!string.IsNullOrEmpty(topic.topicId))
                {
                    topicLookup[topic.topicId] = topic;
                }

                if (topic.lessons == null) continue;

                foreach (var lesson in topic.lessons)
                {
                    allLessons.Add(lesson);
                    if (!string.IsNullOrEmpty(lesson.lessonId))
                    {
                        lessonLookup[lesson.lessonId] = lesson;
                    }

                    if (lesson.activities == null) continue;

                    foreach (var act in lesson.activities)
                    {
                        totalActivityCount++;
                        if (!string.IsNullOrEmpty(act.activityId))
                        {
                            activityLookup[act.activityId] = act;
                        }
                    }
                }
            }

            isLoaded = true;
            Debug.Log($"[CourseDatabase] Kurikulum berhasil dimuat: {dataRoot.topics.Count} Topik, {allLessons.Count} Lesson, {totalActivityCount} Aktivitas.");
        }

        public CourseTopic GetTopic(string topicId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(topicId)) return null;
            topicLookup.TryGetValue(topicId, out var topic);
            return topic;
        }

        public CourseLesson GetLesson(string lessonId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(lessonId)) return null;
            lessonLookup.TryGetValue(lessonId, out var lesson);
            return lesson;
        }

        public CourseLesson GetLessonByIndex(int index)
        {
            EnsureLoaded();
            if (index >= 0 && index < allLessons.Count) return allLessons[index];
            return null;
        }

        public int GetLessonIndex(string lessonId)
        {
            EnsureLoaded();
            return allLessons.FindIndex(l => l.lessonId == lessonId);
        }

        public CourseActivity GetActivity(string activityId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(activityId)) return null;
            activityLookup.TryGetValue(activityId, out var act);
            return act;
        }

        public Texture2D LoadIllustration(string imageName)
        {
            if (string.IsNullOrEmpty(imageName)) return null;

            if (illustrationCache.TryGetValue(imageName, out var cachedTex) && cachedTex != null)
            {
                return cachedTex;
            }

            string path = IllustrationResourceFolder + imageName;
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                Sprite spr = Resources.Load<Sprite>(path);
                if (spr != null) tex = spr.texture;
            }

            if (tex != null)
            {
                illustrationCache[imageName] = tex;
            }
            else
            {
                Debug.LogWarning($"[CourseDatabase] Gambar panel tidak ditemukan: {path}");
            }

            return tex;
        }
    }
}

