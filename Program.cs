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

class Program
{
    static void Main(string[] args)
    {
        var graphe = new Graphe();

        var a = new Station("A");
        var b = new Station("B");
        var c = new Station("C");
        var d = new Station("D");

        graphe.AjouterStation(a);
        graphe.AjouterStation(b);
        graphe.AjouterStation(c);
        graphe.AjouterStation(d);

        graphe.AjouterTrajet(a, b, 60);   // 1 min
        graphe.AjouterTrajet(b, c, 90);   // 1 min 30
        graphe.AjouterTrajet(a, c, 10);  // 5 min
        graphe.AjouterTrajet(c, d, 120);  // 2 min

        var distances = Dijkstra.CalculerChemins(graphe, a, out var predecesseurs);
        var chemin = Dijkstra.TrouverChemin(d, predecesseurs);

        Console.WriteLine("Chemin le plus court de A à D :");
        foreach (var station in chemin)
        {
            Console.Write(station.Nom + " ");
        }

        Console.WriteLine($"\nDurée totale : {distances[d]} secondes");
    }
}