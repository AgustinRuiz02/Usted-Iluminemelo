
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RhythmLaneController : MonoBehaviour
{
    [System.Serializable]
    public class RhythmCharacter
    {
        public string characterName;
        public KeyCode key;
        public Color noteColor = Color.white;
        public SpriteRenderer sprite;
    }

    private class RhythmNote
    {
        public RectTransform rect;
        public Image image;
        public RhythmCharacter character;
        public bool resolved;
    }

    [Header("Carril UI")]
    public RectTransform laneArea;
    public RectTransform hitMarker;
    public Image notePrefab;

    [Header("Personajes y teclas")]
    public RhythmCharacter aura = new RhythmCharacter
    {
        characterName = "Aura",
        key = KeyCode.Q,
        noteColor = Color.blue
    };

    public RhythmCharacter nyx = new RhythmCharacter
    {
        characterName = "Nyx",
        key = KeyCode.W,
        noteColor = Color.red
    };

    public RhythmCharacter pulse = new RhythmCharacter
    {
        characterName = "Pulse",
        key = KeyCode.E,
        noteColor = Color.yellow
    };

    public RhythmCharacter ray = new RhythmCharacter
    {
        characterName = "Ray",
        key = KeyCode.R,
        noteColor = Color.green
    };

    [Header("Cristy")]
    public SpriteRenderer cristySprite;

    [Header("Movimiento de notas")]
    [Min(10f)]
    public float noteSpeed = 350f;

    [Min(0.2f)]
    public float spawnInterval = 1.3f;

    [Min(10f)]
    public float noteSize = 40f;

    [Tooltip("Distancia máxima al marcador para permitir acertar.")]
    [Min(1f)]
    public float hitWindowPixels = 45f;

    [Header("Nivel")]
    [Min(1f)]
    public float levelDuration = 30f;

    [Min(1)]
    public int maxLives = 5;

    [Min(0)]
    public int pointsPerHit = 50;

    [Header("UI del nivel")]
    public Slider progressBar;
    public Text scoreText;
    public Text livesText;
    public Text timerText;

    [Header("Paneles finales")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public Text finalScoreText;

    [Header("Respuesta visual")]
    [Min(0f)]
    public float successDisplayTime = 0.45f;

    public Color missColor = Color.black;

    [Min(0.05f)]
    public float blinkInterval = 0.12f;

    [Min(1)]
    public int blinkCount = 2;

    [Header("Inicio")]
    public bool startAutomatically = true;

    private RhythmCharacter[] characters;

    private readonly List<RhythmNote> notes =
        new List<RhythmNote>();

    private float spawnTimer;
    private float reactionTimer;
    private float blinkTimer;
    private float elapsedTime;

    private int score;
    private int lives;

    private int blinkChanges;

    private bool started;
    private bool reacting;
    private bool blinking;
    private bool blinkIsOn;
    private bool levelFinished;

    private RhythmCharacter missCharacter;

    void Start()
    {
        characters = new RhythmCharacter[]
        {
            aura, nyx, pulse, ray
        };

        lives = maxLives;

        TurnCharactersOff();

        if (cristySprite != null)
            cristySprite.color = Color.white;

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (defeatPanel != null)
            defeatPanel.SetActive(false);

        UpdateUI();

        if (startAutomatically)
            StartGame();
    }

    void Update()
    {
        if (!started)
        {
            if (!levelFinished &&
                Input.GetKeyDown(KeyCode.Space))
            {
                StartGame();
            }

            return;
        }

        // Actualizar duración del nivel.
        elapsedTime += Time.deltaTime;

        UpdateLevelProgress();

        // Comprobar si terminó el nivel.
        if (elapsedTime >= levelDuration)
        {
            WinGame();
            return;
        }

        UpdateNotes();
        CheckInputs();
        UpdateReaction();

        // No generar más notas si el nivel terminó.
        if (!started)
            return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnNote();
            spawnTimer = spawnInterval;
        }
    }

    public void StartGame()
    {
        if (levelFinished)
            return;

        if (characters == null)
        {
            characters = new RhythmCharacter[]
            {
                aura, nyx, pulse, ray
            };
        }

        started = true;
        elapsedTime = 0f;
        spawnTimer = 0.4f;

        score = 0;
        lives = maxLives;

        reacting = false;
        blinking = false;
        missCharacter = null;

        TurnCharactersOff();

        if (cristySprite != null)
            cristySprite.color = Color.white;

        ClearNotes();

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (defeatPanel != null)
            defeatPanel.SetActive(false);

        UpdateUI();

        Debug.Log("¡Comienza el nivel!");
    }

    void SpawnNote()
    {
        if (laneArea == null || notePrefab == null)
        {
            Debug.LogError(
                "Asigná Lane Area y Note Prefab en el Inspector.");

            started = false;
            return;
        }

        RhythmCharacter character =
            characters[Random.Range(0, characters.Length)];

        Image image = Instantiate(notePrefab, laneArea);

        RectTransform rect = image.rectTransform;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.sizeDelta = new Vector2(noteSize, noteSize);

        float startX = laneArea.rect.width * 0.5f
                     + noteSize * 0.5f;

        rect.anchoredPosition = new Vector2(startX, 0f);

        image.color = character.noteColor;

        notes.Add(new RhythmNote
        {
            rect = rect,
            image = image,
            character = character,
            resolved = false
        });
    }

    void UpdateNotes()
    {
        float markerX = GetMarkerX();

        for (int i = notes.Count - 1; i >= 0; i--)
        {
            RhythmNote note = notes[i];

            if (note.resolved)
                continue;

            Vector2 position = note.rect.anchoredPosition;

            position.x -= noteSpeed * Time.deltaTime;

            note.rect.anchoredPosition = position;

            // La nota pasó el marcador sin acertarse.
            if (position.x < markerX - hitWindowPixels)
            {
                ResolveMiss(note);

                if (!started)
                    return;
            }

            // Eliminar notas que salieron del carril.
            if (position.x <
                -laneArea.rect.width * 0.5f - noteSize)
            {
                Destroy(note.image.gameObject);
                notes.RemoveAt(i);
            }
        }
    }

    void CheckInputs()
    {
        for (int c = 0; c < characters.Length; c++)
        {
            if (!Input.GetKeyDown(characters[c].key))
                continue;

            RhythmNote nearestNote = null;

            float nearestDistance = float.MaxValue;
            float markerX = GetMarkerX();

            foreach (RhythmNote note in notes)
            {
                if (note.resolved)
                    continue;

                float distance = Mathf.Abs(
                    note.rect.anchoredPosition.x - markerX);

                if (distance <= hitWindowPixels &&
                    distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNote = note;
                }
            }

            if (nearestNote == null)
            {
                Debug.Log("Pulsación fuera de tiempo.");
                continue;
            }

            if (nearestNote.character == characters[c])
            {
                ResolveHit(nearestNote);
            }
            else
            {
                ResolveMiss(nearestNote);
            }

            return;
        }
    }

    float GetMarkerX()
    {
        if (hitMarker == null || laneArea == null)
            return 0f;

        return laneArea.InverseTransformPoint(
            hitMarker.position).x;
    }

    void ResolveHit(RhythmNote note)
    {
        if (note.resolved || !started)
            return;

        note.resolved = true;

        // Sumar 50 puntos por pose correcta.
        score += pointsPerHit;

        Color successColor = note.character.noteColor;

        TurnCharactersOff();

        if (note.character.sprite != null)
            note.character.sprite.color = successColor;

        if (cristySprite != null)
            cristySprite.color = successColor;

        Destroy(note.image.gameObject);

        Debug.Log(
            "¡ACIerto! " + note.character.characterName +
            " | Puntos: " + score);

        blinking = false;
        missCharacter = null;

        reacting = true;
        reactionTimer = successDisplayTime;

        UpdateUI();
    }

    void ResolveMiss(RhythmNote note)
    {
        if (note.resolved || !started)
            return;

        note.resolved = true;

        // Guardar el personaje de la nota fallida.
        missCharacter = note.character;

        Destroy(note.image.gameObject);

        // Cada fallo resta una vida.
        lives--;

        Debug.Log(
            "¡FALLO! Nota de " + missCharacter.characterName +
            " | Vidas restantes: " + lives);

        reacting = false;
        blinking = true;

        blinkIsOn = false;
        blinkChanges = 0;
        blinkTimer = blinkInterval;

        TurnCharactersOff();

        if (cristySprite != null)
            cristySprite.color = Color.white;

        UpdateUI();

        // Derrota inmediata al quedarse sin vidas.
        if (lives <= 0)
        {
            LoseGame();
        }
    }

    void UpdateReaction()
    {
        if (reacting)
        {
            reactionTimer -= Time.deltaTime;

            if (reactionTimer <= 0f)
            {
                reacting = false;

                TurnCharactersOff();

                if (cristySprite != null)
                    cristySprite.color = Color.white;
            }
        }

        if (blinking)
        {
            blinkTimer -= Time.deltaTime;

            if (blinkTimer <= 0f)
            {
                blinkTimer = blinkInterval;

                blinkIsOn = !blinkIsOn;
                blinkChanges++;

                Color color = blinkIsOn
                    ? missColor
                    : Color.white;

                // Solo parpadea el personaje de la nota fallida.
                if (missCharacter != null &&
                    missCharacter.sprite != null)
                {
                    missCharacter.sprite.color = color;
                }

                // Cristy parpadea al mismo tiempo.
                if (cristySprite != null)
                    cristySprite.color = color;

                if (blinkChanges >= blinkCount * 2)
                {
                    blinking = false;

                    if (missCharacter != null &&
                        missCharacter.sprite != null)
                    {
                        missCharacter.sprite.color = Color.white;
                    }

                    if (cristySprite != null)
                        cristySprite.color = Color.white;

                    missCharacter = null;
                }
            }
        }
    }

    void UpdateLevelProgress()
    {
        float progress = Mathf.Clamp01(
            elapsedTime / levelDuration);

        if (progressBar != null)
            progressBar.value = progress;

        float remainingTime = Mathf.Max(
            0f, levelDuration - elapsedTime);

        int seconds = Mathf.CeilToInt(remainingTime);

        if (timerText != null)
        {
            timerText.text = string.Format(
                "{0:00}:{1:00}",
                seconds / 60,
                seconds % 60);
        }
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Puntos: " + score;

        if (livesText != null)
            livesText.text = "Vidas: " + lives;

        if (progressBar != null)
            progressBar.value = Mathf.Clamp01(
                elapsedTime / levelDuration);

        if (timerText != null)
        {
            float remainingTime = Mathf.Max(
                0f, levelDuration - elapsedTime);

            int seconds = Mathf.CeilToInt(remainingTime);

            timerText.text = string.Format(
                "{0:00}:{1:00}",
                seconds / 60,
                seconds % 60);
        }
    }

    void WinGame()
    {
        if (levelFinished)
            return;

        EndGame(true);
    }

    void LoseGame()
    {
        if (levelFinished)
            return;

        EndGame(false);
    }

    void EndGame(bool victory)
    {
        started = false;
        levelFinished = true;

        reacting = false;
        blinking = false;

        TurnCharactersOff();

        if (cristySprite != null)
            cristySprite.color = Color.white;

        ClearNotes();

        if (victoryPanel != null)
            victoryPanel.SetActive(victory);

        if (defeatPanel != null)
            defeatPanel.SetActive(!victory);

        if (finalScoreText != null)
            finalScoreText.text = "Puntaje final: " + score;

        if (victory)
        {
            if (progressBar != null)
                progressBar.value = 1f;

            if (timerText != null)
                timerText.text = "00:00";

            Debug.Log(
                "¡VICTORIA! Puntaje final: " + score);
        }
        else
        {
            Debug.Log(
                "DERROTA. Puntaje final: " + score);
        }
    }

    void ClearNotes()
    {
        foreach (RhythmNote note in notes)
        {
            if (note.image != null)
                Destroy(note.image.gameObject);
        }

        notes.Clear();
    }

    void TurnCharactersOff()
    {
        if (characters == null)
            return;

        foreach (RhythmCharacter character in characters)
        {
            if (character != null && character.sprite != null)
                character.sprite.color = Color.white;
        }
    }
}
