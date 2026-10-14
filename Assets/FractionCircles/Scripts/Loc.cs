using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FractionCircles
{
    public enum Language { English, French }

    /// <summary>All interface texts in English and French. Switching language updates every bound text live.</summary>
    public static class Loc
    {
        static readonly Dictionary<string, string[]> texts = new Dictionary<string, string[]>
        {
            // Start screen
            ["title"] = new[] { "Fraction Circles", "Cercles de fractions" },
            ["subtitle"] = new[] { "A visual tool for learning fractions", "Un outil visuel pour apprendre les fractions" },
            ["start"] = new[] { "MANIPULATE", "MANIPULER" },
            ["tour"] = new[] { "GUIDED TOUR", "VISITE GUIDÉE" },
            ["exercises"] = new[] { "EXERCISES", "EXERCICES" },
            ["soon"] = new[] { "SOON", "BIENTÔT" },

            // Toolbar
            ["menu"] = new[] { "Menu", "Menu" },
            ["help"] = new[] { "Help", "Aide" },
            ["labels"] = new[] { "Labels", "Noms" },
            ["colours"] = new[] { "Colours", "Couleurs" },
            ["sound"] = new[] { "Sound", "Son" },
            ["language"] = new[] { "Language", "Langue" },
            ["langCode"] = new[] { "EN", "FR" },

            // Table
            ["binHint"] = new[] { "Tap again to clear the table", "Touchez encore pour vider la table" },
            ["tourBanner"] = new[] { "GUIDED TOUR  ·  tap anywhere to stop", "VISITE GUIDÉE  ·  touchez l'écran pour arrêter" },
            ["ok"] = new[] { "OK", "OK" },

            // Help
            ["helpTitle"] = new[] { "How to use", "Mode d'emploi" },
            ["help1"] = new[] { "Drag a piece out of the green tray onto the table, or just tap it.", "Faites glisser une pièce du plateau vert vers la table, ou touchez-la simplement." },
            ["help2"] = new[] { "Drop pieces next to each other: they fit together into a circle.", "Posez les pièces côte à côte : elles s'assemblent pour former un cercle." },
            ["help3"] = new[] { "Tap a piece to turn it (right-click or mouse wheel turns it too).", "Touchez une pièce pour la tourner (clic droit ou molette aussi)." },
            ["help4"] = new[] { "Drag numbers and the fraction bar to write fractions.", "Faites glisser les nombres et la barre de fraction pour écrire des fractions." },
            ["help5"] = new[] { "Numbers snap above or below a fraction bar.", "Les nombres se placent au-dessus ou en dessous de la barre." },
            ["help6"] = new[] { "Drag anything back to the tray or onto the bin to put it away.", "Ramenez un élément sur le plateau ou dans la corbeille pour le ranger." },
            ["help7"] = new[] { "Tap the bin twice to clear the whole table.", "Touchez deux fois la corbeille pour vider toute la table." },
            ["help8"] = new[] { "½ shows the fraction names, the colour wheel gives each size its own colour.", "½ affiche le nom des fractions, la roue de couleurs donne une couleur à chaque taille." },
            ["help9"] = new[] { "FR / EN switches the language.", "FR / EN change la langue." },

            // Exercises
            ["exTitle"] = new[] { "Exercises", "Exercices" },
            ["exBody"] = new[]
            {
                "Coming soon:\n\n" +
                "-  Guided lessons: naming, equivalence, comparing fractions\n" +
                "-  Adding, subtracting, multiplying and dividing with the circles\n" +
                "-  Progressive levels and progress tracking for each student\n" +
                "-  Reports for teachers and parents",
                "Bientôt disponible :\n\n" +
                "-  Leçons guidées : nommer, équivalences, comparer des fractions\n" +
                "-  Additionner, soustraire, multiplier et diviser avec les cercles\n" +
                "-  Niveaux progressifs et suivi de chaque élève\n" +
                "-  Bilans pour les enseignants et les parents",
            },
        };

        public static Language Current
        {
            get
            {
                int stored = PlayerPrefs.GetInt("fc_lang", -1);
                if (stored >= 0) return (Language)stored;
                return Application.systemLanguage == SystemLanguage.French ? Language.French : Language.English;
            }
            set
            {
                PlayerPrefs.SetInt("fc_lang", (int)value);
                Settings.NotifyChanged();
            }
        }

        public static void Toggle() => Current = Current == Language.English ? Language.French : Language.English;

        public static string Get(string key) =>
            texts.TryGetValue(key, out var t) ? t[(int)Current] : key;

        /// <summary>Keeps <paramref name="text"/> showing <paramref name="key"/> in the current language.</summary>
        public static void Bind(Text text, string key) => LocalizedText.Attach(text, key);
    }
}
