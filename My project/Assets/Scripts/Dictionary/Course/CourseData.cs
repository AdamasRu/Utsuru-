using System;
using System.Collections.Generic;

namespace Utsuru.Course
{
    /// <summary>Root JSON container: seluruh kurikulum Irodori A1.</summary>
    [Serializable]
    public class CourseDataRoot
    {
        public List<CourseTopic> topics = new List<CourseTopic>();
    }

    [Serializable]
    public class CourseTopic
    {
        public string topicId;     // IR-A1-T01
        public string titleJp;     // はじめての日本語
        public string titleEn;     // Starting to Speak Japanese
        public string titleId;     // Mulai Berbahasa Jepang
        public string description;
        public List<CourseLesson> lessons = new List<CourseLesson>();
    }

    [Serializable]
    public class CourseLesson
    {
        public string lessonId;    // IR-A1-L01
        public int lessonNumber;   // 1
        public string titleJp;     // おはようございます
        public string titleEn;     // Good morning!
        public string titleId;     // Selamat pagi!
        public string objective;   // Tujuan lesson
        public int priority;       // 0=P0, 1=P1, 2=P2
        public string aksaraNote;  // Catatan aksara (hiragana/katakana intro)
        public List<CourseActivity> activities = new List<CourseActivity>();
        public List<CourseGrammarEntry> grammar = new List<CourseGrammarEntry>();
        public CourseCulturalNote culturalNote;
        public List<string> kanjiWords = new List<string>();
    }

    [Serializable]
    public class CourseActivity
    {
        public string activityId;  // IR-A1-T01-L01-A01
        public string titleJp;     // こんにちは
        public string titleEn;     // Greeting someone
        public string titleId;     // Sapaan saat bertemu
        public string explanation; // Penjelasan dalam bahasa Indonesia / English
        public List<string> keyPhrases = new List<string>();
        /// <summary>Nama file di Resources/CourseIllustrations/ tanpa ekstensi.</summary>
        public List<string> panelImages = new List<string>();
        public int priority;       // 0=P0, 1=P1, 2=P2
        public List<CourseQuizQuestion> quizQuestions = new List<CourseQuizQuestion>();
    }

    [Serializable]
    public class CourseGrammarEntry
    {
        public string grammarId;   // IR-A1-L03-GR01
        public string explanation;
    }

    [Serializable]
    public class CourseCulturalNote
    {
        public string noteId;      // IR-A1-L01-CT01
        public string content;
    }

    [Serializable]
    public class CourseQuizQuestion
    {
        public string question;
        public List<string> options = new List<string>();
        public int correctIndex;
    }
}

