namespace Utsuru.FlashcardSystem
{
    /// <summary>
    /// Status tingkat penguasaan hafalan kartu (PBO Enum).
    /// </summary>
    public enum FlashcardMasteryStatus
    {
        New = 0,        // Kartu baru dibuat, belum pernah dipelajari
        Learning = 1,   // Sedang dipelajari (terakhir dijawab belum hafal)
        Mastered = 2    // Sudah hafal / dikuasai (terakhir dijawab sudah hafal)
    }
}
