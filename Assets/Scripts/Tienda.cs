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
    // Optional UI texts for other power-ups (assign in Inspector if desired)
    public Text time_up_text;
    public Text inmunidad_text;
    public Text frenzy_time_text;

    void Start()
    {
        // Load saved values (if any)
        Moneda = PlayerPrefs.GetInt("MonedasTotales", 0);
        pw_x2 = PlayerPrefs.GetInt("pw_x2", 0);
        time_up = PlayerPrefs.GetInt("time_up", 0);
        inmunidad = PlayerPrefs.GetInt("inmunidad", 0);
        frenzy_time = PlayerPrefs.GetInt("frenzy_time", 0);

        // Update UI if assigned
        if (moneda_text != null) moneda_text.text = Moneda.ToString();
        if (pw_x2_text != null) pw_x2_text.text = pw_x2.ToString();
        if (time_up_text != null) time_up_text.text = time_up.ToString();
        if (inmunidad_text != null) inmunidad_text.text = inmunidad.ToString();
        if (frenzy_time_text != null) frenzy_time_text.text = frenzy_time.ToString();
    }

    public void buy_pw_x2()
    {
        int monedasActuales = PlayerPrefs.GetInt("MonedasTotales", 0);
        if (monedasActuales >= 20)
        {
            monedasActuales -= 20;
            // Save updated currency to PlayerPrefs (single source of truth)
            PlayerPrefs.SetInt("MonedasTotales", monedasActuales);

            // Increase power-up count and save
            pw_x2 += 1;
            PlayerPrefs.SetInt("pw_x2", pw_x2);
            PlayerPrefs.Save();

            // Update local cached value and UI if assigned
            Moneda = monedasActuales;
            if (moneda_text != null) moneda_text.text = Moneda.ToString();
            if (pw_x2_text != null) pw_x2_text.text = pw_x2.ToString();
        }
        else
        {
            print("No tienes suficientes monedas");
        }
    }

        public void buy_time_up()
    {
            int monedasActuales = PlayerPrefs.GetInt("MonedasTotales", 0);
            if (monedasActuales >= 15)
            {
                monedasActuales -= 15;
                // Save updated currency to PlayerPrefs (single source of truth)
                PlayerPrefs.SetInt("MonedasTotales", monedasActuales);

                // Increase time_up and save
                frenzy_time += 1;
                PlayerPrefs.SetInt("frenzy_time", frenzy_time);
                PlayerPrefs.Save();

                // Update local cached value and UI if assigned
                Moneda = monedasActuales;
                if (moneda_text != null) moneda_text.text = Moneda.ToString();
                if (time_up_text != null) time_up_text.text = time_up.ToString();
            }
        else
        {
            print("No tienes suficientes monedas");
        }
    }


        public void buy_inmunidad()
    {
        int monedasActuales = PlayerPrefs.GetInt("MonedasTotales", 0);
        if (monedasActuales >= 30)
        {
            monedasActuales -= 30;
            // Save updated currency to PlayerPrefs (single source of truth)
            PlayerPrefs.SetInt("MonedasTotales", monedasActuales);

            // Increase power-up count and save
            inmunidad += 1;
            PlayerPrefs.SetInt("inmunidad", inmunidad);
            PlayerPrefs.Save();

            // Update local cached value and UI if assigned
            Moneda = monedasActuales;
            if (moneda_text != null) moneda_text.text = Moneda.ToString();
            if (inmunidad_text != null) inmunidad_text.text = inmunidad.ToString();
        }
        else
        {
            print("No tienes suficientes monedas");
        }
    }



            public void buy_frenzy_time()
    {
        int monedasActuales = PlayerPrefs.GetInt("MonedasTotales", 0);
        if (monedasActuales >= 10)
        {
            monedasActuales -= 10;
            // Save updated currency to PlayerPrefs (single source of truth)
            PlayerPrefs.SetInt("MonedasTotales", monedasActuales);

            // Increase power-up count and save
            frenzy_time += 1;
            PlayerPrefs.SetInt("frenzy_time", frenzy_time);
            PlayerPrefs.Save();

            // Update local cached value and UI if assigned
            Moneda = monedasActuales;
            if (moneda_text != null) moneda_text.text = Moneda.ToString();
            if (frenzy_time_text != null) frenzy_time_text.text = frenzy_time.ToString();
        }
        else
        {
            print("No tienes suficientes monedas");
        }
    }
}
