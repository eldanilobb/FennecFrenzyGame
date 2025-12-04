using UnityEngine;
using UnityEngine.UI;

public class Tienda : MonoBehaviour
{
    public int Moneda;
    public int pw_x2;
    public int time_up;
    public int inmunidad;
    public int frenzy_time;
    public Text moneda_text;
    public Text pw_x2_text;

    void Start()
    {
        Moneda = PlayerPrefs.GetInt("MonedasTotales", 0);
        moneda_text.text = Moneda.ToString();
    }

    public void buy_pw_x2()
    {
        if(Moneda >= 20)
        {
            Moneda -= 20;
            PlayerPrefs.SetInt("MonedasTotales", Moneda);
            moneda_text.text = Moneda.ToString();

            pw_x2 += 1;
            pw_x2_text.text = pw_x2.ToString();
        }
        else
        {
            print("No tienes suficientes monedas");
        }
    }
}
