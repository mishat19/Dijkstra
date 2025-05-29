// ============================
// CLASSES METIER
// ============================
using MySql.Data.MySqlClient;

class Station
{
    public string Nom { get; set; }

    public Station(string nom)
    {
        Nom = nom;
    }
}

class Trajet
{
    public Station Depart { get; set; }
    public Station Arrivee { get; set; }
    public int DureeEnSecondes { get; set; }
    public List<int> Lignes { get; set; }

    public Trajet(Station depart, Station arrivee, int duree, List<int> lignes)
    {
        Depart = depart;
        Arrivee = arrivee;
        DureeEnSecondes = duree;
        Lignes = lignes;
    }
}

class Graphe
{
    public List<Station> Stations { get; set; } = new List<Station>();
    public List<Trajet> Trajets { get; set; } = new List<Trajet>();

    public void AjouterStation(Station station)
    {
        Stations.Add(station);
    }

    public void AjouterTrajet(Station depart, Station arrivee, int duree, List<int> lignes)
    {
        Trajets.Add(new Trajet(depart, arrivee, duree, lignes));
    }

    public List<Trajet> ObtenirTrajetsDepuis(Station station)
    {
        return Trajets.Where(t => t.Depart == station).ToList();
    }
}

class Dijkstra
{
    public static Dictionary<Station, int> CalculerChemins(Graphe graphe, Station depart, out Dictionary<Station, Station> predecesseurs)
    {
        var distances = new Dictionary<Station, int>();
        predecesseurs = new Dictionary<Station, Station>();
        var aVisiter = new List<Station>();

        foreach (var station in graphe.Stations)
        {
            distances[station] = int.MaxValue;
            aVisiter.Add(station);
        }

        distances[depart] = 0;

        while (aVisiter.Count > 0)
        {
            var stationActuelle = aVisiter.OrderBy(s => distances[s]).First();
            aVisiter.Remove(stationActuelle);

            foreach (var trajet in graphe.ObtenirTrajetsDepuis(stationActuelle))
            {
                int nouvelleDistance = distances[stationActuelle] + trajet.DureeEnSecondes;

                if (nouvelleDistance < distances[trajet.Arrivee])
                {
                    distances[trajet.Arrivee] = nouvelleDistance;
                    predecesseurs[trajet.Arrivee] = stationActuelle;
                }
            }
        }

        return distances;
    }

    public static List<Station> TrouverChemin(Station arrivee, Dictionary<Station, Station> predecesseurs)
    {
        var chemin = new List<Station>();
        var station = arrivee;

        while (predecesseurs.ContainsKey(station))
        {
            chemin.Add(station);
            station = predecesseurs[station];
        }

        chemin.Add(station);
        chemin.Reverse();
        return chemin;
    }
}

class ChargeurGrapheDepuisMySQL
{
    public static Graphe Charger(string chaineConnexion)
    {
        var graphe = new Graphe();
        var stations = new Dictionary<int, Station>();

        using (var connexion = new MySqlConnection(chaineConnexion))
        {
            connexion.Open();

            var commandeStation = new MySqlCommand("SELECT NumStation, NomStation FROM Station", connexion);
            using (var lecteur = commandeStation.ExecuteReader())
            {
                while (lecteur.Read())
                {
                    int id = lecteur.GetInt32("NumStation");
                    string nom = lecteur.GetString("NomStation");

                    var station = new Station(nom);
                    graphe.AjouterStation(station);
                    stations[id] = station;
                }
            }

            var commandeSegment = new MySqlCommand(@"
                SELECT s.NumStationA, s.NumStationB, s.DureeSegment, l.NumLigne
                FROM Segment s
                JOIN Ligne l ON s.NumSegment = l.NumSegment", connexion);

            using (var lecteur = commandeSegment.ExecuteReader())
            {
                var trajetsTemp = new Dictionary<(int, int), (int duree, List<int> lignes)>();

                while (lecteur.Read())
                {
                    int idA = lecteur.GetInt32("NumStationA");
                    int idB = lecteur.GetInt32("NumStationB");
                    int duree = lecteur.GetInt32("DureeSegment");
                    int numLigne = lecteur.GetInt32("NumLigne");

                    var cle = (idA, idB);
                    if (!trajetsTemp.ContainsKey(cle))
                        trajetsTemp[cle] = (duree, new List<int>());

                    trajetsTemp[cle].lignes.Add(numLigne);
                }

                foreach (var kvp in trajetsTemp)
                {
                    var (idA, idB) = kvp.Key;
                    var (duree, lignes) = kvp.Value;

                    if (stations.ContainsKey(idA) && stations.ContainsKey(idB))
                    {
                        graphe.AjouterTrajet(stations[idA], stations[idB], duree, lignes);
                    }
                }
            }
        }

        return graphe;
    }
}

class Program
{
    static void Main(string[] args)
    {
        string serveur = "10.1.139.236";
        string login = "b3";
        string mdp = "mdp";
        string bd = "baseb3";

        string chaineConnexion = $"server={serveur};uid={login};pwd={mdp};database={bd}";
        var graphe = ChargeurGrapheDepuisMySQL.Charger(chaineConnexion);

        var depart = graphe.Stations.FirstOrDefault(s => s.Nom == "Romolo");
        var arrivee = graphe.Stations.FirstOrDefault(s => s.Nom == "Cordusio");

        if (depart == null || arrivee == null)
        {
            Console.WriteLine("Stations non trouvées.");
            return;
        }

        var distances = Dijkstra.CalculerChemins(graphe, depart, out var predecesseurs);
        var chemin = Dijkstra.TrouverChemin(arrivee, predecesseurs);

        Console.WriteLine($"Chemin le plus court de {depart.Nom} à {arrivee.Nom} :");

        int? lignePrecedente = null;
        for (int i = 0; i < chemin.Count - 1; i++)
        {
            var stationA = chemin[i];
            var stationB = chemin[i + 1];

            var trajet = graphe.Trajets.FirstOrDefault(t => t.Depart == stationA && t.Arrivee == stationB);
            if (trajet != null)
            {
                int ligneActuelle = trajet.Lignes.First();

                if (ligneActuelle != lignePrecedente)
                {
                    if (lignePrecedente != null)
                        Console.WriteLine("↳ Changement de ligne");
                    Console.WriteLine($"Prendre la ligne {ligneActuelle}");
                    lignePrecedente = ligneActuelle;
                }

                Console.WriteLine($" → {stationB.Nom}");
            }
        }

        Console.WriteLine($"\nDurée totale : {distances[arrivee]} secondes");
    }
}
