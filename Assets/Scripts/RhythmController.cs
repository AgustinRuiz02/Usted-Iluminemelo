
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

    private int score;
    private int misses;
    private int blinkChanges;

    private bool started;
    private bool reacting;
    private bool blinking;
    private bool blinkIsOn;

    // Personaje específico de la nota fallida.
    private RhythmCharacter missCharacter;

    void Start()
    {
        characters = new RhythmCharacter[]
        {
            aura, nyx, pulse, ray
        };

        TurnCharactersOff();

        if (cristySprite != null)
            cristySprite.color = Color.white;

        if (startAutomatically)
            StartGame();
    }

    void Update()
    {
        if (!started)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                StartGame();

            return;
        }

        UpdateNotes();
        CheckInputs();
        UpdateReaction();

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnNote();
            spawnTimer = spawnInterval;
        }
    }

    public void StartGame()
    {
        if (characters == null)
        {
            characters = new RhythmCharacter[]
            {
                aura, nyx, pulse, ray
            };
        }

        started = true;
        spawnTimer = 0.4f;

        Debug.Log("¡Comienza el carril rítmico!");
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
            }

            // Eliminar notas que salieron completamente del carril.
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

            // No hay ninguna nota en la zona de pulsación.
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
        if (note.resolved)
            return;

        note.resolved = true;
        score++;

        Color successColor = note.character.noteColor;

        // Solo se ilumina el personaje correcto.
        TurnCharactersOff();

        if (note.character.sprite != null)
            note.character.sprite.color = successColor;

        if (cristySprite != null)
            cristySprite.color = successColor;

        Destroy(note.image.gameObject);

        Debug.Log(
            "¡ACIerto! " + note.character.characterName +
            " | Puntos: " + score);

        // Cancelar cualquier parpadeo anterior.
        blinking = false;
        missCharacter = null;

        reacting = true;
        reactionTimer = successDisplayTime;
    }

    void ResolveMiss(RhythmNote note)
    {
        if (note.resolved)
            return;

        note.resolved = true;
        misses++;

        // Guardamos el personaje de la nota que se falló.
        missCharacter = note.character;

        Destroy(note.image.gameObject);

        Debug.Log(
            "¡FALLO! Nota de " + missCharacter.characterName +
            " | Fallos: " + misses);

        reacting = false;
        blinking = true;
        blinkIsOn = false;
        blinkChanges = 0;
        blinkTimer = blinkInterval;

        // Apagar todos los personajes al comenzar el fallo.
        TurnCharactersOff();

        if (cristySprite != null)
            cristySprite.color = Color.white;
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

                // Cristy parpadea sincronizado con ese personaje.
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
