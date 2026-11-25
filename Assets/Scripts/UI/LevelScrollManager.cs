using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ParkMaster.Data;

public class LevelScrollManager : MonoBehaviour
{
    [Header("Prefab & Container")]
    [SerializeField] private LevelItemCard levelCardPrefab;

    [SerializeField] private Transform contentParent;

    [SerializeField] private ScrollRect scrollRect;

    [Header("Thumbnail Sprites (tùy chọn)")]
    [SerializeField] private List<Sprite> levelThumbnails = new List<Sprite>();

    public const string KEY_CURRENT_LEVEL  = "ParkMaster_CurrentLevel";
    public const string KEY_UNLOCKED_LEVEL = "ParkMaster_MaxUnlockedLevel";
    public const string KEY_STARS_PREFIX   = "ParkMaster_Stars_L";
    public const string KEY_LENGTH_PREFIX  = "ParkMaster_Len_L";

    private readonly List<LevelItemCard> _cards = new List<LevelItemCard>();

    private void Awake()
    {
        FindReferences();
    }

    public void RefreshCards()
    {
        FindReferences();

        ClearCards();
        BuildCards();
        StartCoroutine(ScrollToCurrentLevel());
    }

    private void BuildCards()
    {
        if (levelCardPrefab == null)
        {
            Debug.LogError("[LevelScrollManager] levelCardPrefab chưa gán! Kéo LevelItemCard.prefab vào Inspector.");
            return;
        }
        if (contentParent == null)
        {
            Debug.LogError("[LevelScrollManager] contentParent null! Hãy kéo ô Content của ScrollView vào Inspector.");
            return;
        }

        int total = GetTotalLevels();
        int maxUnlocked = GetMaxUnlockedLevel();

        int firstUncompletedLevel = -1;
        for (int i = 1; i <= total; i++)
        {
            int s = SaveSystem.Current.GetInt(SaveKeys.LevelStars(i), 0);
            if (s == 0 && i <= maxUnlocked)
            {
                firstUncompletedLevel = i;
                break;
            }
        }

        for (int i = 1; i <= total; i++)
        {
            int   stars  = SaveSystem.Current.GetInt(SaveKeys.LevelStars(i), 0);
            float length = SaveSystem.Current.GetFloat(SaveKeys.LevelLength(i), 0f);

            LevelCardState state;
            if (stars > 0)
            {

                state = LevelCardState.Completed;
            }
            else if (i == firstUncompletedLevel)
            {

                state = LevelCardState.Current;
            }
            else if (i <= maxUnlocked)
            {
                state = (firstUncompletedLevel == -1 && i == maxUnlocked)
                    ? LevelCardState.Current
                    : LevelCardState.Completed;
            }
            else
            {
                state = LevelCardState.Locked;
            }

            Sprite thumb = (levelThumbnails != null && i - 1 < levelThumbnails.Count)
                           ? levelThumbnails[i - 1]
                           : null;

            int childIdx = i - 1;
            LevelItemCard card = null;
            if (childIdx < contentParent.childCount)
            {
                Transform child = contentParent.GetChild(childIdx);
                child.gameObject.SetActive(true);
                card = child.GetComponent<LevelItemCard>();
            }

            if (card == null)
            {
                card = Instantiate(levelCardPrefab, contentParent);
            }

            card.Setup(i, state, stars, length, thumb);
            _cards.Add(card);
        }

        for (int i = total; i < contentParent.childCount; i++)
        {
            contentParent.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void ClearCards()
    {
        _cards.Clear();
    }

    private IEnumerator ScrollToCurrentLevel()
    {

        yield return null;
        yield return null;

        if (scrollRect == null || _cards.Count == 0) yield break;

        int current = SaveSystem.Current.GetInt(SaveKeys.CurrentLevel, 1);
        int idx = Mathf.Clamp(current - 1, 0, _cards.Count - 1);

        if (idx == 0)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            yield break;
        }

        RectTransform contentRt = contentParent as RectTransform;
        RectTransform cardRt    = _cards[idx].GetComponent<RectTransform>();
        if (contentRt == null || cardRt == null) yield break;

        float contentH = contentRt.rect.height;
        float cardY    = Mathf.Abs(cardRt.anchoredPosition.y);
        scrollRect.verticalNormalizedPosition = 1f - Mathf.Clamp01(cardY / Mathf.Max(contentH, 1f));
    }

    public static int GetMaxUnlockedLevel()
    {
        int unlocked = SaveSystem.Current.GetInt(SaveKeys.MaxUnlockedLevel, 1);
        int current  = SaveSystem.Current.GetInt(SaveKeys.CurrentLevel, 1);
        return Mathf.Max(unlocked, current);
    }

    public static void UnlockNextLevel(int nextLevel)
    {
        int maxUnlocked = GetMaxUnlockedLevel();
        if (nextLevel > maxUnlocked)
        {
            SaveSystem.Current.SetInt(SaveKeys.MaxUnlockedLevel, nextLevel);
            SaveSystem.Current.Save();
        }
    }

    public static void SaveLevelResult(int levelIndex, int starsEarned, float totalLength)
    {
        SaveSystem.Current.SetInt(SaveKeys.LevelStars(levelIndex), starsEarned);
        if (totalLength > 0f)
        {
            SaveSystem.Current.SetFloat(SaveKeys.LevelLength(levelIndex), totalLength);
        }

        UnlockNextLevel(levelIndex + 1);
        SaveSystem.Current.Save();
    }

    public static int GetStars(int levelIndex)
        => SaveSystem.Current.GetInt(SaveKeys.LevelStars(levelIndex), 0);

    [ContextMenu("Reset All Level Data")]
    public static void ResetAllLevelData()
    {
        int total = 100;
        if (LevelManager.Instance != null)
            total = LevelManager.Instance.GetTotalLevelsCount();

        for (int i = 1; i <= total; i++)
        {
            SaveSystem.Current.DeleteKey(SaveKeys.LevelStars(i));
            SaveSystem.Current.DeleteKey(SaveKeys.LevelLength(i));
        }

        SaveSystem.Current.SetInt(SaveKeys.CurrentLevel, 1);
        SaveSystem.Current.SetInt(SaveKeys.MaxUnlockedLevel, 1);
        SaveSystem.Current.Save();

        Debug.Log("<color=orange>[LevelScrollManager] All level progression data reset.</color>");

        var mgr = FindFirstObjectByType<LevelScrollManager>(FindObjectsInactive.Include);
        if (mgr != null && mgr.gameObject.activeInHierarchy)
        {
            mgr.RefreshCards();
        }
    }

    private void FindReferences()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>(true);
        }

        if (contentParent == null && scrollRect != null)
        {
            contentParent = scrollRect.content;
        }
    }

    private int GetTotalLevels()
    {
        if (LevelManager.Instance != null)
            return LevelManager.Instance.GetTotalLevelsCount();
        return 10;
    }
}
