using MySql.Data.MySqlClient;

class Station
{
    public string Nom { get; set; }

    /// <summary>
    /// Objet permettant de crée un objet station
    /// </summary>
    /// <param name="nom">Nom de la station</param>
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

    /// <summary>
    /// Objet permettant de crée un objet trajet
    /// </summary>
    /// <param name="depart">La station de départ du trajet</param>
    /// <param name="arrivee">La station d'arriver du trajet</param>
    /// <param name="duree">La durée du trajet</param>
    public Trajet(Station depart, Station arrivee, int duree)
    {
        Depart = depart;
        Arrivee = arrivee;
        DureeEnSecondes = duree;
    }
}

class Graphe
{
    public List<Station> Stations { get; set; } = new List<Station>();
    public List<Trajet> Trajets { get; set; } = new List<Trajet>();

    /// <summary>
    /// Fonction permettant d'ajouter une station au graphe
    /// </summary>
    /// <param name="station">Objet station qui contient les informations de la station</param>
    public void AjouterStation(Station station)
    {
        Stations.Add(station);
    }

    /// <summary>
    /// Fonction permettant d'ajouter un trajet au graphe de type Trajet
    /// </summary>
    /// <param name="depart"></param>
    /// <param name="arrivee"></param>
    /// <param name="duree"></param>
    public void AjouterTrajet(Station depart, Station arrivee, int duree)
    {
        Trajets.Add(new Trajet(depart, arrivee, duree));
    }

    /// <summary>
    /// Fonction permettant d'obtenir tous les trajets depuis une station donnée
    /// </summary>
    /// <param name="station">Objet station ayant les information des station</param>
    /// <returns>Retourne le fichier le plus court</returns>
    public List<Trajet> ObtenirTrajetsDepuis(Station station)
    {
        return Trajets.Where(t => t.Depart == station).ToList();
    }
}

class Dijkstra
{
    /// <summary>
    /// Fonction permettant de calculer les chemins les plus courts à partir d'une station de départ
    /// </summary>
    /// <param name="graphe">Graphe des stations</param>
    /// <param name="depart">Station de départ</param>
    /// <param name="predecesseurs">Stations avant la station de départ</param>
    /// <returns></returns>
    public static Dictionary<Station, int> CalculerChemins(Graphe graphe, Station depart, out Dictionary<Station, Station> predecesseurs)
    {
        var distances = new Dictionary<Station, int>();
        predecesseurs = new Dictionary<Station, Station>();
        var aVisiter = new List<Station>();

        // Initialiser les distances
        foreach (var station in graphe.Stations)
        {
            distances[station] = int.MaxValue;
            aVisiter.Add(station);
        }

        distances[depart] = 0;

        while (aVisiter.Count > 0)
        {
            // Station avec la plus petite distance
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

    /// <summary>
    /// Fonction permettant de trouver le chemin le plus court à partir des prédecesseurs
    /// </summary>
    /// <param name="arrivee">Station d'arrivé</param>
    /// <param name="predecesseurs">Station avant la station de départ</param>
    /// <returns></returns>
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

            // Charger les stations
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

            // Charger les segments (trajets)
            var commandeSegment = new MySqlCommand("SELECT NumStationA, NumStationB, DureeSegment FROM Segment", connexion);
            using (var lecteur = commandeSegment.ExecuteReader())
            {
                while (lecteur.Read())
                {
                    int idA = lecteur.GetInt32("NumStationA");
                    int idB = lecteur.GetInt32("NumStationB");
                    int duree = lecteur.GetInt32("DureeSegment");

                    if (stations.ContainsKey(idA) && stations.ContainsKey(idB))
                    {
                        var stationA = stations[idA];
                        var stationB = stations[idB];

                        // Uniquement dans un sens (pas bidirectionnel comme tu l'as demandé)
                        graphe.AjouterTrajet(stationA, stationB, duree);
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

        // On saisit les stations de départ et d'arrivée
        var depart = graphe.Stations.FirstOrDefault(s => s.Nom == "Pagano");
        var arrivee = graphe.Stations.FirstOrDefault(s => s.Nom == "Pasteur");

        // Si la station de départ/d'arrivée n'existe pas, on affiche un message d'erreur
        if (depart == null || arrivee == null)
        {
            Console.WriteLine("Stations non trouvées.");
            return;
        }

        // On calcule et cherche le chemin
        var distances = Dijkstra.CalculerChemins(graphe, depart, out var predecesseurs);
        var chemin = Dijkstra.TrouverChemin(arrivee, predecesseurs);

        // Retourne le resultat
        Console.WriteLine($"Chemin le plus court de {depart.Nom} à {arrivee.Nom} :");
        foreach (var station in chemin)
        {
            Console.Write(station.Nom + " ");
        }

        Console.WriteLine($"\nDurée totale : {distances[arrivee]} secondes");
    }
}
