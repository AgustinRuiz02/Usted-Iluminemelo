
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RhythmLaneController : MonoBehaviour
{
    // =====================================================
    // PERSONAJES
    // =====================================================

    [System.Serializable]
    public class RhythmCharacter
    {
        public string characterName;
        public KeyCode key;
        public Color noteColor;
        public SpriteRenderer sprite;
    }

    private class RhythmNote
    {
        public RectTransform rectTransform;
        public Image image;

        public RhythmCharacter character;
        public RhythmCharacter character2;

        public RhythmCharacter pendingCharacter;
        public float pendingTime;
    }

    [Header("Personajes")]
    public RhythmCharacter aura;
    public RhythmCharacter nyx;
    public RhythmCharacter pulse;
    public RhythmCharacter ray;

    public SpriteRenderer cristySprite;

    private RhythmCharacter[] characters;

    // =====================================================
    // PANTALLAS PREVIAS Y FINALES
    // =====================================================

    [Header("Paneles de navegación")]
    public GameObject comicPanel;
    public GameObject loadingPanel;
    public GameObject tutorialPanel;

    [Header("Paneles de resultados")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;

    [Header("Nombre de la escena del menú")]
    public string menuSceneName = "Menu";

    // =====================================================
    // ELEMENTOS DEL GAMEPLAY
    // =====================================================

    [Header("Elementos del gameplay en el Canvas")]
    public RectTransform laneArea;
    public Slider progressBar;
    public Text scoreText;
    public Text livesText;
    public Text timerText;
    public Text comboText;

    // =====================================================
    // CONFIGURACIÓN DE NOTAS
    // =====================================================

    [Header("Configuración de notas")]
    public RectTransform hitMarker;
    public Image notePrefab;

    public float noteSpeed = 350f;
    public float spawnInterval = 1.3f;
    public float noteSize = 40f;
    public float hitWindowPixels = 45f;

    [Header("Poses combinadas")]
    [Range(0f, 1f)]
    public float combinedNoteChance = 0.25f;

    public int pointsPerCombinedHit = 100;

    [Tooltip("Tiempo máximo entre las dos teclas.")]
    public float chordWindow = 0.15f;

    private List<RhythmNote> activeNotes = new List<RhythmNote>();

    private float spawnTimer = 0f;

    // =====================================================
    // CONFIGURACIÓN DEL NIVEL
    // =====================================================

    [Header("Configuración del nivel")]
    public float levelDuration = 30f;
    public int maxLives = 5;
    public int pointsPerHit = 50;

    private int score = 0;
    private int lives = 0;
    private int combo = 0;

    private float elapsedTime = 0f;

    private bool gameStarted = false;
    private bool levelFinished = false;

    // =====================================================
    // RESULTADOS
    // =====================================================

    [Header("Textos de resultados")]
    public Text victoryScoreText;
    public Text defeatScoreText;

    // =====================================================
    // REACCIONES VISUALES
    // =====================================================

    [Header("Duración de las reacciones")]
    public float successDisplayTime = 0.45f;

    [Header("Parpadeo al fallar")]
    public Color missColor = Color.black;
    public float blinkInterval = 0.12f;
    public int blinkCount = 2;

    private float reactionTimer = 0f;
    private float blinkTimer = 0f;

    private bool isBlinking = false;
    private bool blinkVisible = false;
    private bool successReaction = false;

    private int currentBlinkCount = 0;

    private RhythmCharacter reactionCharacter;
    private RhythmCharacter reactionCharacter2;

    // =====================================================
    // INICIO
    // =====================================================

    private void Start()
    {
        characters = new RhythmCharacter[]
        {
            aura,
            nyx,
            pulse,
            ray
        };

        RestoreCharacterColors();

        ShowComicScreen();

        UpdateUI();
    }

    private void Update()
    {
        if (!gameStarted || levelFinished)
            return;

        elapsedTime += Time.deltaTime;

        UpdateLevelProgress();
        UpdateNotes();

        if (levelFinished)
            return;

        CheckInputs();

        if (levelFinished)
            return;

        UpdateReaction();

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            SpawnNote();
            spawnTimer = 0f;
        }

        if (elapsedTime >= levelDuration)
        {
            WinGame();
        }
    }

    // =====================================================
    // NAVEGACIÓN ENTRE PANELES
    // =====================================================

    private void ShowComicScreen()
    {
        gameStarted = false;
        levelFinished = false;

        SetPanel(comicPanel, true);
        SetPanel(loadingPanel, false);
        SetPanel(tutorialPanel, false);

        SetPanel(victoryPanel, false);
        SetPanel(defeatPanel, false);

        SetGameplayUI(false);
    }

    public void OnComicContinue()
    {
        SetPanel(comicPanel, false);
        SetPanel(loadingPanel, true);
    }

    public void OnLoadingContinue()
    {
        SetPanel(loadingPanel, false);
        SetPanel(tutorialPanel, true);
    }

    public void OnTutorialContinue()
    {
        SetPanel(tutorialPanel, false);

        SetGameplayUI(true);

        StartGame();
    }

    private void SetPanel(GameObject panel, bool state)
    {
        if (panel != null)
            panel.SetActive(state);
    }

    // =====================================================
    // MOSTRAR / OCULTAR GAMEPLAY
    // =====================================================

    private void SetGameplayUI(bool state)
    {
        if (laneArea != null)
            laneArea.gameObject.SetActive(state);

        if (progressBar != null)
            progressBar.gameObject.SetActive(state);

        if (scoreText != null)
            scoreText.gameObject.SetActive(state);

        if (livesText != null)
            livesText.gameObject.SetActive(state);

        if (timerText != null)
            timerText.gameObject.SetActive(state);

        if (comboText != null)
            comboText.gameObject.SetActive(state);
    }

    // =====================================================
    // INICIAR PARTIDA
    // =====================================================

    public void StartGame()
    {
        ClearNotes();

        score = 0;
        lives = maxLives;
        combo = 0;

        elapsedTime = 0f;
        spawnTimer = 0f;

        gameStarted = true;
        levelFinished = false;

        isBlinking = false;
        successReaction = false;

        reactionCharacter = null;
        reactionCharacter2 = null;

        RestoreCharacterColors();

        if (progressBar != null)
            progressBar.value = 0f;

        UpdateUI();
    }

    // =====================================================
    // VOLVER AL MENÚ
    // =====================================================

    public void ReturnToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    // =====================================================
    // GENERAR NOTAS
    // =====================================================

    private void SpawnNote()
    {
        if (laneArea == null || notePrefab == null || characters == null)
            return;

        if (characters.Length < 2)
            return;

        Image newNote = Instantiate(notePrefab, laneArea);

        RectTransform noteRect = newNote.rectTransform;

        noteRect.sizeDelta = new Vector2(noteSize, noteSize);

        float spawnX = laneArea.rect.width / 2f + noteSize;

        noteRect.anchoredPosition = new Vector2(spawnX, 0f);

        RhythmNote rhythmNote = new RhythmNote();

        rhythmNote.rectTransform = noteRect;
        rhythmNote.image = newNote;

        bool createCombinedNote =
            Random.value < combinedNoteChance;

        if (createCombinedNote)
        {
            int firstIndex = Random.Range(0, characters.Length);

            int secondIndex = Random.Range(0, characters.Length - 1);

            if (secondIndex >= firstIndex)
                secondIndex++;

            rhythmNote.character = characters[firstIndex];
            rhythmNote.character2 = characters[secondIndex];

            CreateCombinedVisual(
                newNote,
                rhythmNote.character.noteColor,
                rhythmNote.character2.noteColor
            );
        }
        else
        {
            rhythmNote.character =
                characters[Random.Range(0, characters.Length)];

            newNote.color = rhythmNote.character.noteColor;
        }

        activeNotes.Add(rhythmNote);
    }

    // =====================================================
    // VISUAL DE DOS MITADES
    // =====================================================

    private void CreateCombinedVisual(
        Image rootImage,
        Color firstColor,
        Color secondColor)
    {
        // Ocultamos la imagen original del prefab.
        rootImage.enabled = false;

        Sprite noteSprite = rootImage.sprite;

        // Mitad izquierda.
        Image leftImage = CreateHalfImage(
            rootImage.rectTransform,
            noteSprite,
            "LeftColor",
            firstColor
        );

        RectTransform leftRect = leftImage.rectTransform;

        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.5f, 1f);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;

        // Mitad derecha.
        Image rightImage = CreateHalfImage(
            rootImage.rectTransform,
            noteSprite,
            "RightColor",
            secondColor
        );

        RectTransform rightRect = rightImage.rectTransform;

        rightRect.anchorMin = new Vector2(0.5f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;
    }

    private Image CreateHalfImage(
        RectTransform parent,
        Sprite sprite,
        string objectName,
        Color color)
    {
        GameObject halfObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        halfObject.transform.SetParent(parent, false);

        Image halfImage = halfObject.GetComponent<Image>();

        halfImage.sprite = sprite;
        halfImage.color = color;
        halfImage.raycastTarget = false;
        halfImage.type = Image.Type.Simple;

        return halfImage;
    }

    // =====================================================
    // MOVIMIENTO DE NOTAS
    // =====================================================

    private void UpdateNotes()
    {
        float markerX = GetMarkerX();

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            RhythmNote note = activeNotes[i];

            if (note == null || note.rectTransform == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            Vector2 position = note.rectTransform.anchoredPosition;

            position.x -= noteSpeed * Time.deltaTime;

            note.rectTransform.anchoredPosition = position;

            if (position.x < markerX - hitWindowPixels)
            {
                ResolveMiss(note);

                if (levelFinished)
                    return;
            }
        }
    }

    private float GetMarkerX()
    {
        if (hitMarker == null)
            return 0f;

        return hitMarker.anchoredPosition.x;
    }

    // =====================================================
    // DETECCIÓN DE TECLAS
    // =====================================================

    private void CheckInputs()
    {
        float markerX = GetMarkerX();

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            RhythmNote note = activeNotes[i];

            if (note == null || note.rectTransform == null)
                continue;

            float distance = Mathf.Abs(
                note.rectTransform.anchoredPosition.x - markerX
            );

            if (distance > hitWindowPixels)
                continue;

            bool firstKeyDown =
                Input.GetKeyDown(note.character.key);

            // ---------------------------------------------
            // POSE NORMAL
            // ---------------------------------------------

            if (note.character2 == null)
            {
                if (firstKeyDown)
                {
                    ResolveHit(note);
                    return;
                }

                continue;
            }

            // ---------------------------------------------
            // POSE COMBINADA
            // ---------------------------------------------

            bool secondKeyDown =
                Input.GetKeyDown(note.character2.key);

            // Si se presionan las dos teclas en el mismo frame.
            if (firstKeyDown && secondKeyDown)
            {
                ResolveHit(note);
                return;
            }

            // Si la primera tecla presionada ya expiró.
            if (note.pendingCharacter != null &&
                Time.time - note.pendingTime > chordWindow)
            {
                note.pendingCharacter = null;
            }

            // Primera tecla.
            if (note.pendingCharacter == null)
            {
                if (firstKeyDown)
                {
                    note.pendingCharacter = note.character;
                    note.pendingTime = Time.time;
                    return;
                }

                if (secondKeyDown)
                {
                    note.pendingCharacter = note.character2;
                    note.pendingTime = Time.time;
                    return;
                }
            }
            else
            {
                // Ya se presionó la primera: esperamos la otra.
                if (note.pendingCharacter == note.character &&
                    secondKeyDown)
                {
                    ResolveHit(note);
                    return;
                }

                if (note.pendingCharacter == note.character2 &&
                    firstKeyDown)
                {
                    ResolveHit(note);
                    return;
                }
            }
        }
    }

    // =====================================================
    // ACIERTO
    // =====================================================

    private void ResolveHit(RhythmNote note)
    {
        if (note == null || !activeNotes.Contains(note))
            return;

        // Puntaje según el tipo de pose.
        if (note.character2 != null)
            score += pointsPerCombinedHit;
        else
            score += pointsPerHit;

        combo++;

        activeNotes.Remove(note);

        if (note.image != null)
            Destroy(note.image.gameObject);

        reactionCharacter = note.character;
        reactionCharacter2 = note.character2;

        successReaction = true;
        isBlinking = false;

        reactionTimer = successDisplayTime;

        // Primer personaje.
        if (reactionCharacter != null &&
            reactionCharacter.sprite != null)
        {
            reactionCharacter.sprite.enabled = true;
            reactionCharacter.sprite.color =
                reactionCharacter.noteColor;
        }

        // Segundo personaje, si es una pose combinada.
        if (reactionCharacter2 != null &&
            reactionCharacter2.sprite != null)
        {
            reactionCharacter2.sprite.enabled = true;
            reactionCharacter2.sprite.color =
                reactionCharacter2.noteColor;
        }

        // Cristy toma el color normal o la mezcla de ambos.
        if (cristySprite != null)
        {
            cristySprite.enabled = true;

            if (reactionCharacter2 != null)
            {
                cristySprite.color = Color.Lerp(
                    reactionCharacter.noteColor,
                    reactionCharacter2.noteColor,
                    0.5f
                );
            }
            else
            {
                cristySprite.color =
                    reactionCharacter.noteColor;
            }
        }

        UpdateUI();
    }

    // =====================================================
    // ERROR
    // =====================================================

    private void ResolveMiss(RhythmNote note)
    {
        if (note == null || !activeNotes.Contains(note))
            return;

        activeNotes.Remove(note);

        if (note.image != null)
            Destroy(note.image.gameObject);

        lives--;

        combo = 0;

        reactionCharacter = note.character;
        reactionCharacter2 = note.character2;

        successReaction = false;
        isBlinking = true;

        blinkVisible = true;
        currentBlinkCount = 0;

        blinkTimer = 0f;

        // Primer personaje.
        if (reactionCharacter != null &&
            reactionCharacter.sprite != null)
        {
            reactionCharacter.sprite.enabled = true;
            reactionCharacter.sprite.color = missColor;
        }

        // Segundo personaje, si corresponde.
        if (reactionCharacter2 != null &&
            reactionCharacter2.sprite != null)
        {
            reactionCharacter2.sprite.enabled = true;
            reactionCharacter2.sprite.color = missColor;
        }

        if (cristySprite != null)
        {
            cristySprite.enabled = true;
            cristySprite.color = missColor;
        }

        UpdateUI();

        if (lives <= 0)
        {
            LoseGame();
        }
    }

    // =====================================================
    // ACTUALIZAR REACCIONES
    // =====================================================

    private void UpdateReaction()
    {
        if (successReaction)
        {
            reactionTimer -= Time.deltaTime;

            if (reactionTimer <= 0f)
            {
                successReaction = false;
                RestoreCharacterColors();
            }

            return;
        }

        if (!isBlinking)
            return;

        blinkTimer += Time.deltaTime;

        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;

            blinkVisible = !blinkVisible;

            Color blinkColor =
                blinkVisible ? missColor : Color.white;

            if (reactionCharacter != null &&
                reactionCharacter.sprite != null)
            {
                reactionCharacter.sprite.enabled = true;
                reactionCharacter.sprite.color = blinkColor;
            }

            if (reactionCharacter2 != null &&
                reactionCharacter2.sprite != null)
            {
                reactionCharacter2.sprite.enabled = true;
                reactionCharacter2.sprite.color = blinkColor;
            }

            if (cristySprite != null)
            {
                cristySprite.enabled = true;
                cristySprite.color = blinkColor;
            }

            if (!blinkVisible)
            {
                currentBlinkCount++;

                if (currentBlinkCount >= blinkCount)
                {
                    isBlinking = false;
                    RestoreCharacterColors();
                }
            }
        }
    }

    // =====================================================
    // RESTAURAR COLORES
    // =====================================================

    private void RestoreCharacterColors()
    {
        if (characters != null)
        {
            foreach (RhythmCharacter character in characters)
            {
                if (character != null && character.sprite != null)
                {
                    character.sprite.enabled = true;
                    character.sprite.color = Color.white;
                }
            }
        }

        if (cristySprite != null)
        {
            cristySprite.enabled = true;
            cristySprite.color = Color.white;
        }
    }

    // =====================================================
    // PROGRESO DEL NIVEL
    // =====================================================

    private void UpdateLevelProgress()
    {
        if (levelDuration <= 0f)
            return;

        float progress = Mathf.Clamp01(elapsedTime / levelDuration);

        if (progressBar != null)
            progressBar.value = progress;

        UpdateUI();
    }

    // =====================================================
    // ACTUALIZAR INTERFAZ
    // =====================================================

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Puntaje: " + score.ToString();

        if (livesText != null)
            livesText.text = "Vidas: " + lives.ToString();

        if (timerText != null)
        {
            float remainingTime =
                Mathf.Max(0f, levelDuration - elapsedTime);

            timerText.text = "Tiempo: " +
                Mathf.CeilToInt(remainingTime).ToString();
        }

        if (comboText != null)
            comboText.text = "Combo: x" + combo.ToString();

        if (progressBar != null && levelDuration > 0f)
        {
            progressBar.value =
                Mathf.Clamp01(elapsedTime / levelDuration);
        }
    }

    // =====================================================
    // VICTORIA Y DERROTA
    // =====================================================

    private void WinGame()
    {
        EndGame(true);
    }

    private void LoseGame()
    {
        EndGame(false);
    }

    private void EndGame(bool victory)
    {
        if (levelFinished)
            return;

        levelFinished = true;
        gameStarted = false;

        ClearNotes();

        isBlinking = false;
        successReaction = false;

        RestoreCharacterColors();

        SetPanel(comicPanel, false);
        SetPanel(loadingPanel, false);
        SetPanel(tutorialPanel, false);

        SetGameplayUI(false);

        SetPanel(victoryPanel, victory);
        SetPanel(defeatPanel, !victory);

        if (victory)
        {
            if (victoryScoreText != null)
            {
                victoryScoreText.text =
                    "Puntaje final: " + score.ToString();

                Debug.Log("Puntaje final asignado: " + score);
            }
            else
            {
                Debug.LogWarning(
                    "Victory Score Text no está asignado en el Inspector."
                );
            }
        }
        else
        {
            if (defeatScoreText != null)
            {
                defeatScoreText.text =
                    "Puntaje final: " + score.ToString();

                Debug.Log("Puntaje final asignado: " + score);
            }
            else
            {
                Debug.LogWarning(
                    "Defeat Score Text no está asignado en el Inspector."
                );
            }
        }

        UpdateUI();
    }

    // =====================================================
    // LIMPIAR NOTAS
    // =====================================================

    private void ClearNotes()
    {
        foreach (RhythmNote note in activeNotes)
        {
            if (note != null && note.image != null)
                Destroy(note.image.gameObject);
        }

        activeNotes.Clear();
    }
}
