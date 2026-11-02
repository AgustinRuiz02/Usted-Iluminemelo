using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escenas

public class MainMenuController : MonoBehaviour
{
    // Método que llamará el botón
    public void Jugar()
    {
        // Reemplaza "NombreDeTuEscenaDelJuego" con el nombre exacto de la escena de tu juego
        SceneManager.LoadScene("SampleScene");
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
