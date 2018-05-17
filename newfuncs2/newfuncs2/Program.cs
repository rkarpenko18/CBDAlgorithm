using Accord.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace newfuncs
{
    //source: https://stackoverflow.com/questions/20530128/how-to-find-all-partitions-of-a-set
    //This class contains methods for creating partitions of an IEnumerable.
    public static class Partitioning
    {
        public static IEnumerable<T[][]> GetAllPartitions<T>(T[] elements)
        {
            return GetAllPartitions(new T[][] { }, elements);
        }

        private static IEnumerable<T[][]> GetAllPartitions<T>(
            T[][] fixedParts, T[] suffixElements)
        {
            // A trivial partition consists of the fixed parts
            // followed by all suffix elements as one block
            yield return fixedParts.Concat(new[] { suffixElements }).ToArray();

            // Get all two-group-partitions of the suffix elements
            // and sub-divide them recursively
            var suffixPartitions = GetTuplePartitions(suffixElements);
            foreach (Tuple<T[], T[]> suffixPartition in suffixPartitions)
            {
                var subPartitions = GetAllPartitions(
                    fixedParts.Concat(new[] { suffixPartition.Item1 }).ToArray(),
                    suffixPartition.Item2);
                foreach (var subPartition in subPartitions)
                {
                    yield return subPartition;
                }
            }
        }

        private static IEnumerable<Tuple<T[], T[]>> GetTuplePartitions<T>(
            T[] elements)
        {
            // No result if less than 2 elements
            if (elements.Length < 2) yield break;

            // Generate all 2-part partitions
            for (int pattern = 1; pattern < 1 << (elements.Length - 1); pattern++)
            {
                // Create the two result sets and
                // assign the first element to the first set
                List<T>[] resultSets = {
                    new List<T> { elements[0] }, new List<T>() };
                // Distribute the remaining elements
                for (int index = 1; index < elements.Length; index++)
                {
                    resultSets[(pattern >> (index - 1)) & 1].Add(elements[index]);
                }

                yield return Tuple.Create(
                    resultSets[0].ToArray(), resultSets[1].ToArray());
            }
        }
    }
    class Program
    {
        //to be used as a delegate, to determine if all pairings/groups of Groups in a combination have 3 or fewer groups.
        //Passed into Where() function to query the result of GetAllPartitions()
        private static bool lessThanFour(Group[][] combo)
        {
            foreach (Group[] bus in combo)
            {
                if (bus.Count() >= 4)
                {
                    return false;
                }
            }
            return true;
        }

        //Data structure representing the result of the k-means cluster. Contains a list of groups in the cluster and
        //a data structure to hold the combinations of that members List
        public class Cluster : IComparable<Cluster>
        {
            public List<Group> members;
            public List<Group[][]> combos; //List of combinations (partitions) of the members object. Each combination is an array of pairings (think buses). Each pairing is 1-3 groups in their own array

            public Cluster()
            {
                members = new List<Group>();
                combos = new List<Group[][]>();
            }

            //call this constructor when splitting a cluster into 2 smaller ones, since you can pass in the new members
            public Cluster(List<Group> m)
            {
                members = new List<Group>();
                combos = new List<Group[][]>();
                members = m;
            }

            //So that Clusters can be sorted. It sorts them based on how many members they have since that is how we order them later
            public int CompareTo(Cluster other)
            {
                if (this.members.Count < other.members.Count)
                {
                    return -1;
                }
                else if (this.members.Count == other.members.Count)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        //A group has a destination to which they are traveling, as well as the number of students in the group.
        public class Group
        {
            public Location destination;
            public int cluster;             //initial cluster that is assigned. If that cluster is split then it gets the value of the new cluster
            public int numStudents;

            public Group(Location dest, int c, int num)
            {
                destination = dest;
                cluster = c;
                numStudents = num;
            }
        }

        public class Location
        {
            public string address;
            public Dictionary<string, double> distTo;   //takes in the address of a different Location and relates it to the time between them. NOTE: DREW SAID IT WORKS FOR LOCATION INSTEAD OF STRING
            public double latitude;
            public double longitude;
            public double[] coords = new double[2];     //the translated unit values of the lat and long. Used to cluster the groups by angle from whitworth.

            public Location(string addr, string city, string state, double lat, double lon, double xcoord, double ycoord)
            {
                address = addr + " " + city + ", " + state;
                latitude = lat;
                longitude = lon;
                coords[0] = xcoord;
                coords[1] = ycoord;
                distTo = new Dictionary<string, double>();
            }
        }

        public class Bus : IComparable<Bus>
        {
            public int totalSeats;
            public List<Group> groups;
            public bool filled = false;     //the bus has been given members (not in the potentialBuses List)

            //Used to sort buses based on total number of seats
            public int CompareTo(Bus other)
            {
                //using totalSeats because we sort the buses when they are empty
                if (this.totalSeats < other.totalSeats)
                {
                    return -1;
                }
                else if (this.totalSeats == other.totalSeats)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            //total number of seats being used by the bus. Pass busindex, clusterindex, comboindex as -1 if the potentialBuses list should not be considered
            public double seatsTaken(List<List<int>> potBus, int busindex, int clusterindex, int comboindex)
            {
                //all groups actually in Bus.groups
                double sum = 0;
                foreach (Group g in groups)
                {
                    sum += g.numStudents;
                }
                //if we want to include the groups that COULD be added to the bus, if those groups' combination is found to be a legal one
                if (busindex != -1 && clusterindex != -1 && comboindex != -1)
                {
                    //we could have some groups that are in potentialBus but are NOT YET in the Bus itself. We must include these too
                    for (int clump = 0; clump < potBus.Count(); clump++)
                    {
                        for (int group = 0; group < potBus[clump].Count(); group++)
                        {
                            if (potBus[clump][group] == busindex)
                            {
                                sum += clusters[clusterindex].combos[comboindex][clump][group].numStudents;
                            }
                            else
                            {
                                //Console.WriteLine("LoopFail");
                            }
                        }
                    }
                }
                return sum;
            }
            //total seats minus the seats that have been taken
            public double seatsRemaining(List<List<int>> potBus, int busindex, int clusterindex, int comboindex)
            {
                return totalSeats - seatsTaken(potBus, busindex, clusterindex, comboindex);
            }

            public Bus(int seats)
            {
                totalSeats = seats;
                groups = new List<Group>();
            }
        }

        //will hold the indexes of clusters, in the order of the groups List
        static int[] kmeansArr = new int[41];  //num of groups

        //sets the values in kmeansArr[]
        public static void runKMeans(ref Group[] gs)
        {
            int numGroups = gs.Count();
            // Declaring and intializing array for K-Means
            double[][] observations = new double[numGroups][];

            for (int i = 0; i < observations.Length; i++)
            {
                observations[i] = new double[2];
                observations[i][0] = gs[i].destination.coords[0];
                observations[i][1] = gs[i].destination.coords[1];
            }

            KMeans km = new KMeans(7);

            KMeansClusterCollection clust = km.Learn(observations);

            kmeansArr = clust.Decide(observations);

            //output for testing
            /*for (int i = 0; i < kmeansArr.Length; i++)
            {
                Console.WriteLine(dests[i].address + ": " + kmeansArr[i]);
            }*/
        }

        //create the actual Clusters based on kmeansArr[]
        public static void kMeansToClusters()
        {
            //the index of kmeansArr will correspond to the index of groups[], so just assign them across
            for (int i = 0; i < kmeansArr.Length; i++)
            {
                groups[i].cluster = kmeansArr[i];
                clusters[kmeansArr[i]].members.Add(groups[i]);  //add group to corresponding cluster
            }
        }

        //if a cluster is larger than 10, it is very computationally intensive to find the partitions for it. So we want to split up clusters with members > 10
        //this solution is pretty elegant, and it makes sure that even newly generated clusters and previously split clusters are < 10
        public static void findLargeClusters()
        {
            for (int i = 0; i < clusters.Count; i++)
            {
                while (clusters[i].members.Count > 10)   //even if it splits once, will check it again to see if it is still > 10
                {
                    //split cluster
                    splitCluster(i);
                }
            }
        }

        private static void splitCluster(int index)
        {
            //kmeans with k = 2 for clusters[index].members
            //if kmeansArr == 1, add group to temp list for new cluster, delete from clusters[index]
            //create new Cluster with the temp list, append to end of clusters (the for loop in findLargeClusters will adapt to it and check it later for >15
            int numGroups = clusters[index].members.Count;
            double[][] observations = new double[numGroups][];

            for (int i = 0; i < observations.Length; i++)
            {
                observations[i] = new double[2];
                observations[i][0] = clusters[index].members[i].destination.coords[0];
                observations[i][1] = clusters[index].members[i].destination.coords[1];
            }

            KMeans km = new KMeans(2);

            KMeansClusterCollection clust = km.Learn(observations);

            int[] clustArr = clust.Decide(observations);

            //if a group is in the second of the two clusters, we will put it in a new List and delete it from the old one
            List<Group> forNewCluster = new List<Group>();
            for (int i = clustArr.Length - 1; i >= 0; i--)
            {
                if (clustArr[i] == 1)
                {
                    forNewCluster.Add(clusters[index].members[i]);
                    clusters[index].members.RemoveAt(i);
                }
            }

            Cluster newCluster = new Cluster(forNewCluster);
            
            //update the cluster attributes in each group for the new cluster
            clusters.Add(newCluster);
            foreach (Group g in clusters[clusters.Count()-1].members)
            {
                g.cluster = clusters.Count() - 1;
            }
        }

        //since we are clustering the groups and only assigning buses within those clusters, some buses will be left with only one group when they may have fit in a bus that
        //is currently being used in a different cluster. So for single-group buses, we check the surrounding buses and see if we can move that group while staying within the
        //time and capacity limits. If there are multiple candidates, it chooses the bus which would take the least travel time
        public static void moveOnes()
        {
            double shortestTime = 100;
            int newBus = -1;
            //foreach bus, if numGroups == 1, then look for buses NOT in its current cluster that it fits in and time is ok. Record bus with lowest total journey time
            for (int i = 0; i < buses.Count; i++)
            {
                shortestTime = 100;
                newBus = -1;
                //if we have a single-group bus
                if (buses[i].groups.Count == 1)
                {
                    //look for a candidate bus
                    for (int b = 0; b < buses.Count; b++)
                    {
                        //not putting group into same bus AND new potential bus has 1 or 2 groups AND not putting bus into same cluster (earlier condition short circuits)
                        if (b != i && (buses[b].groups.Count == 1 || buses[b].groups.Count == 2) && buses[i].groups[0].cluster != buses[b].groups[0].cluster)
                        {
                            //if fits in bus
                            if (buses[i].groups[0].numStudents < buses[b].seatsRemaining(potentialBuses, -1, -1, -1))
                            {
                                //if one group on new bus. we split these cases up because totalTime is overloaded with a different number of arguments
                                if (buses[b].groups.Count == 1)
                                {
                                    double journeyTime = totalTime(buses[b].groups[0], buses[i].groups[0]);
                                    if (journeyTime <= 40 && journeyTime < shortestTime)
                                    {
                                        shortestTime = journeyTime;
                                        newBus = b;
                                    }
                                }
                                //if two groups on new bus
                                else
                                {
                                    double journeyTime = totalTime(buses[b].groups[0], buses[b].groups[1], buses[i].groups[0]);
                                    if (journeyTime <= 40 && journeyTime < shortestTime)
                                    {
                                        shortestTime = journeyTime;
                                        newBus = b;
                                    }
                                }
                            }
                        }
                    }
                    //if we can move it to a bus with a shorter time, then do it
                    if (newBus != -1)
                    {
                        //put on bus AKA add to new bus member list, delete from old bus member list
                        //buses[i].groups[0].cluster = buses[b].groups[0].cluster;  LINE NOT NECESSARY, WE ARE DONE WITH CLUSTERS NOW?
                        buses[newBus].groups.Add(buses[i].groups[0]);
                        buses[i].groups.RemoveAt(0);    //MAKE SURE THESE TWO LINES WORK PROPERLY RE: REFERENTIAL EQUALITY
                    }
                }
            }
        }

        //journey time for one group on a bus. Will just be the time it takes to get to that one location
        public static double totalTime(Group B)
        {
            //groups[0] == whitworth?
            Group A = groups[0];
            //only combo (AB)
            return groups[0].destination.distTo[B.destination.address];
        }

        //There are two ways to get to two destinations from an origin. Return the fastest one.
        public static double totalTime(Group B, Group C)
        {
            Group A = groups[0];
            double minTime = 100;
            //first combo (ABC)
            double ABC;
            ABC = A.destination.distTo[B.destination.address] + B.destination.distTo[C.destination.address];
            if (ABC < minTime) minTime = ABC;
            //second combo (ACB)
            double ACB;
            ACB = A.destination.distTo[C.destination.address] + C.destination.distTo[B.destination.address];
            if (ACB < minTime) minTime = ACB;

            return minTime;
        }

        //There are six ways to get to three destinations from an origin. Return the fastest one.
        public static double totalTime(Group B, Group C, Group D)
        {
            Group A = groups[0];
            double minTime = 100;
            //ABCD
            double ABCD;
            ABCD = A.destination.distTo[B.destination.address] + B.destination.distTo[C.destination.address] + C.destination.distTo[D.destination.address];
            if (ABCD < minTime) minTime = ABCD;
            //ABDC
            double ABDC;
            ABDC = A.destination.distTo[B.destination.address] + B.destination.distTo[D.destination.address] + D.destination.distTo[C.destination.address];
            if (ABDC < minTime) minTime = ABDC;
            //ACBD
            double ACBD;
            ACBD = A.destination.distTo[C.destination.address] + C.destination.distTo[B.destination.address] + B.destination.distTo[D.destination.address];
            if (ACBD < minTime) minTime = ACBD;
            //ACDB
            double ACDB;
            ACDB = A.destination.distTo[C.destination.address] + C.destination.distTo[D.destination.address] + D.destination.distTo[B.destination.address];
            if (ACDB < minTime) minTime = ACDB;
            //ADBC
            double ADBC;
            ADBC = A.destination.distTo[D.destination.address] + D.destination.distTo[B.destination.address] + B.destination.distTo[C.destination.address];
            if (ADBC < minTime) minTime = ADBC;
            //ADCB
            double ADCB;
            ADCB = A.destination.distTo[D.destination.address] + D.destination.distTo[C.destination.address] + C.destination.distTo[B.destination.address];
            if (ADCB < minTime) minTime = ADCB;

            return minTime;
        }


        //essentially initializes the 'combos' element of a Cluster. Pass in the index of the cluster
        public static void createCombinations(int c)
        {
            //call the Partitioning class.
            //Following line explanation:
            //GetAllPartitions returns an IEnumerable. Takes in a list of Groups, which is the members property of a Cluster.
            //The Where() clause makes it only return the clusters that have no pairings with more than three groups.
            IEnumerable<Group[][]> thePartitions = Partitioning.GetAllPartitions<Group>(clusters[c].members.ToArray()).Where(lessThanFour);    //YOOOOO THIS WORKS
            clusters[c].combos = thePartitions.ToList();    //since GetAllPartitions returns an array, we turn it back to a list (List<Group[][]>)

            //output
            /*
            foreach (Group[][] combo in clusters[c].combos)
            {
                foreach (Group[] bus in combo)
                {
                    Console.Write("[");
                    foreach (Group group in bus)
                    {
                        Console.Write(group.destination + ",");
                    }
                    Console.Write("],");
                }
                Console.Write("\n");
            }
            */

            //output number of combinations. useful to see the load being put on the CPU
            //Console.WriteLine("number of combinations: " + clusters[c].combos.Count);
        }


        //looks through the combinations that have been created too see which ones: 1. Have buses that all take <= 40 minutes... 2. Fit on buses
        public static void fillBuses()
        {
            //sort clusters descending
            //https://stackoverflow.com/questions/3062513/how-can-i-sort-generic-list-desc-and-asc
            clusters.Sort((a, b) => -1 * a.CompareTo(b));    //should call the CompareTo in the Cluster class (based in IComparable)

            //sort combos within each cluster: fewest groups in combo, then by most 3's ( make a custom < function for Group[][] )
            foreach (Cluster c in clusters)
            {
                c.combos = c.combos.OrderBy<Group[][], int>(combo => combo.Count()).ThenByDescending<Group[][], int>(combo => combo.Where(thebus => thebus.Count() == 3).Count()).ToList();
            }

            //sort buses by capacity low to high
            buses.Sort();

            double timeTaken = 100;
            //for each cluster, move through the 'combos', and if it fits onto some unused buses within time limit, then assign them to those buses!
            for (int cl = 0; cl < clusters.Count; cl++)
            {
                //for each combination in the cluster. A new combination will be tried if the previous one was found to break any of the requirements
                for (int cb = 0; cb < clusters[cl].combos.Count(); cb++)
                {
                    //does it fit the time limit? if not, continue
                    bool fitsTimeLimit = true;
                    //TIME LIMIT CHECKING
                    //foreach clump (basically a bus) of groups in a combo
                    for (int clump = 0; clump < clusters[cl].combos[cb].Count(); clump++)
                    {
                        timeTaken = 100;
                        //since totalTime is overloaded
                        switch (clusters[cl].combos[cb][clump].Count())
                        {
                            case 1:
                                timeTaken = totalTime(clusters[cl].combos[cb][clump][0]);
                                break;
                            case 2:
                                timeTaken = totalTime(clusters[cl].combos[cb][clump][0], clusters[cl].combos[cb][clump][1]);
                                break;
                            case 3:
                                timeTaken = totalTime(clusters[cl].combos[cb][clump][0], clusters[cl].combos[cb][clump][1], clusters[cl].combos[cb][clump][2]);
                                break;
                            default:    //will this ever happen?
                                Console.WriteLine("somehow a clump had <1 or >3 groups");
                                break;
                        }
                        //if it breaks the condition, change fitsTimeLimit
                        if (timeTaken > 40)
                        {
                            Console.WriteLine("timeTaken: " + timeTaken + "fail");
                            fitsTimeLimit = false;
                            break;
                        }
                        else
                        {
                            Console.WriteLine("timeTaken: " + timeTaken + " success");
                        }
                    }
                    //after every 'clump' has been tested. If all of them passed the time limit then fitsTimeLimit will still be true
                    if (fitsTimeLimit == false)
                    {
                        continue;
                    }
                    //Console.WriteLine("All clumps in the combo made it through!");
                    
                    //then does it fit in some buses that we have? if not, continue(??)
                    //go through buses. Assign the groups to the buses if they will all fit.
                    //note: currently the clumps of groups within each combo are NOT sorted.
                    //  ^i dont think this will make a huge difference. 
                    potentialBuses.Clear();
                    //potentialBuses represents ONE combination. First list is clump, second is groups. It is used so we don't assign groups to buses before ALL the clumps in a combo have been checked
                    //it is structured the same way as ONE combination from clusters[cl].combos.  So a group's assigned bus is represented by potentialBuses[clump][group]

                    //since we need to restart with potentialBuses every time we reach a new combo, we must recreate the size of it as well for that new combo. Initialize with -1, meaning no bus assigned for that group
                    for (int clumpp = 0; clumpp < clusters[cl].combos[cb].Count(); clumpp++)
                    {
                        potentialBuses.Add(new List<int>());                                //add a 'clump'
                        for (int g = 0; g < clusters[cl].combos[cb][clumpp].Count(); g++)    //add a 'group'
                        {
                            potentialBuses[clumpp].Add(-1);
                        }
                    }
                    
                    //FIT ON BUS CHECK
                    //foreach clump of groups in a combo
                    for (int clump = 0; clump < clusters[cl].combos[cb].Count(); clump++)
                    {
                        //go thru buses to see if it will fit on one of dem. If it fits, then save the index of the bus for the case that they ALL fit
                        //"If it fits, group sits" -Drew
                        //clumpSum represents the total size of the groups in a clump (or bus pairing)
                        int clumpSum = 0;
                        for (int g = 0; g < clusters[cl].combos[cb][clump].Count(); g++)
                        {
                            clumpSum += clusters[cl].combos[cb][clump][g].numStudents;
                        }
                        //see if this clump will fit on a bus.
                        for (int bus = 0; bus < buses.Count(); bus++)
                        {
                            //if the bus' groups attribute does not contain groups AND the bus does not have a potential group assigned to it in potentialBuses
                            if (buses[bus].filled == false && !busTaken(potentialBuses, bus))
                            {
                                //if the clump will fit on the bus
                                if (clumpSum <= buses[bus].seatsRemaining(potentialBuses, bus, cl, cb))
                                {
                                    for (int g = 0; g < clusters[cl].combos[cb][clump].Count(); g++)
                                    {
                                        //use potential buses
                                        potentialBuses[clump][g] = bus;     //assign the bus to the group in potentialBuses. Not actually added to Bus.groups yet
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    //if all the clumps in the combo have been assigned a bus
                    if(busTaken(potentialBuses, -1) == false)
                    {
                        //add the groups to buses using potentialBuses. Set Bus.filled to true
                        for(int clump = 0; clump < potentialBuses.Count(); clump++)
                        {
                            for(int g = 0; g < potentialBuses[clump].Count(); g++)
                            {
                                buses[potentialBuses[clump][g]].groups.Add(clusters[cl].combos[cb][clump][g]);
                                buses[potentialBuses[clump][g]].filled = true;
                            }
                        }
                        break;  //we found a solution for this combo so go to the next cluster
                    }
                    //else try the next combo
                    else
                    {
                        potentialBuses.Clear();
                        
                    }
                }
            }
        }

        //has a group been assigned to a bus in potentialBuses?
        public static bool busTaken(List<List<int>> potBuses, int element)
        {
            for (int clump = 0; clump < potBuses.Count(); clump++)
            {
                for (int groop = 0; groop < potBuses[clump].Count(); groop++)
                {
                    if (potBuses[clump][groop] == element)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void outputBuses()
        {
            for (int bus = 0; bus < buses.Count(); bus++)
            {
                Console.Write("Bus " + bus + " Capacity " + buses[bus].totalSeats + " seats rem: " + buses[bus].seatsRemaining(potentialBuses, -1, -1, -1) + ",");
                for (int group = 0; group < buses[bus].groups.Count(); group++)
                {
                    Console.Write(" Group of " + " size " + buses[bus].groups[group].numStudents + /*" Location " + buses[bus].groups[group].destination.address + */",");
                }
                Console.Write("\n");
                for (int group = 0; group < buses[bus].groups.Count(); group++)
                {
                    Console.Write(buses[bus].groups[group].destination.address + "\n");
                }
                //we are getting some times of 41. Does this mean 41 is somehow the max, or can any time > 41 be ok and we are just coincidentally getting a few at 41?
                switch (buses[bus].groups.Count())
                {
                    case (1):
                        Console.WriteLine("Travel time: " + totalTime(buses[bus].groups[0]));
                        break;
                    case (2):
                        Console.WriteLine("Travel time: " + totalTime(buses[bus].groups[0], buses[bus].groups[1]));
                        break;
                    case (3):
                        Console.WriteLine("Travel time: " + totalTime(buses[bus].groups[0], buses[bus].groups[1], buses[bus].groups[2]));
                        break;
                    default:
                        break;
                }
                Console.Write("\n");
            }
        }

        //List<Group> groups = new List<Group>(40);     //changed it to array to accomodate the partition func. Thought this was fine because we only access indexes of 'groups'
        static Group[] groups = new Group[41];                 //needs to be the right number of groups
        static List<Cluster> clusters = new List<Cluster>();  //number of clusters
        static List<List<int>> potentialBuses = new List<List<int>>();
        static List<Bus> buses = new List<Bus>(30); //needs to be the right number of buses
        static List<Location> locations = new List<Location>(41);
        static bool validSolution = true;

        static void Main(string[] args)
        {
            Accord.Math.Random.Generator.Seed = 0;

            //initialize locations
            {
                Location[] locArray = new Location[41] {new Location("300 W Hawthorne Road", " Spokane", " WA", 47.753226, -117.4162864, 0.707106781, -0.707106781),
                                                        new Location("101 E Hartson", " Spokane", " WA", 47.6502749, -117.4093034, 0.706901259, -0.707312244),
                                                        new Location("101 E Hawthorne Road", " Spokane", " WA", 47.7518581, -117.4085967, 0.707120314, -0.707093249),
                                                        new Location("1015 W 5th Avenue", " Spokane", " WA", 47.6511206, -117.4272376, 0.70686467, -0.70734881),
                                                        new Location("10814 E Broadway Avenue", " Spokane", " WA", 47.664045, -117.258979, 0.707252703, -0.706960829),
                                                        new Location("1120 W Sprague Avenue", " Spokane", " WA", 47.6574937, -117.429075, 0.70687439, -0.707339096),
                                                        new Location("1212 N Howard Street", " Spokane", " WA", 47.6685233, -117.4209265, 0.706915466, -0.707298044),
                                                        new Location("1234 E Front Avenue", " Spokane", " WA", 47.6600366, -117.3912548, 0.706960819, -0.707252713),
                                                        new Location("12509 N Market Street", " Mead", " WA", 47.7700005, -117.3549549, 0.707273974, -0.706939549),
                                                        new Location("130 E 3rd Avenue", " Spokane", " WA", 47.6533357, -117.4085624, 0.706909403, -0.707304104),
                                                        new Location("14000 N Dartford Drive", " Spokane", " WA", 47.7852658, -117.4099818, 0.707188848, -0.707024705),
                                                        new Location("1404 N Ash", " Spokane", " WA", 47.6702639, -117.43653, 0.706885788, -0.707327705),
                                                        new Location("1417 E Hartson", " Spokane", " WA", 47.6508043, -117.3892043, 0.706945431, -0.707268094),
                                                        new Location("1509 W College", " Spokane", " WA", 47.6632874, -117.4342015, 0.706875827, -0.70733766),
                                                        new Location("1523 W Dean Avenue", " Spokane", " WA", 47.6659798, -117.4350038, 0.706879877, -0.707333612),
                                                        new Location("1635 W 26th Avenue", " Spokane", " WA", 47.6308705, -117.4377618, 0.706798743, -0.707414685),
                                                        new Location("19 W Pacific Avenue", " Spokane", " WA", 47.6554384, -117.4122972, 0.706905911, -0.707307595),
                                                        new Location("1906 E Mission Avenue", " Spokane", " WA", 47.6714258, -117.3822379, 0.707004523, -0.707209024),
                                                        new Location("19619 E Cataldo Avenue", " Liberty Lake", " WA", 47.668485, -117.1424019, 0.707511976, -0.706701354),
                                                        new Location("222 E Indiana", " Spokane", " WA", 47.6747681, -117.4077004, 0.706957162, -0.707256369),
                                                        new Location("2316 W First Avenue", " Spokane", " WA", 47.6568363, -117.4468732, 0.706834875, -0.707378582),
                                                        new Location("2410 N Monroe Street", " Spokane", " WA", 47.6798115, -117.4264061, 0.706927915, -0.707285602),
                                                        new Location("25 W 5th Avenue", " Spokane", " WA", 47.6511063, -117.4126728, 0.706895825, -0.707317674),
                                                        new Location("2600 W Sharp", " Spokane", " WA", 47.669537, -117.4505749, 0.706854162, -0.70735931),
                                                        new Location("2706 E Queen Avenue", " Spokane", " WA", 47.7039418, -117.3705427, 0.7070992, -0.707114362),
                                                        new Location("318 S Cedar Street", " Spokane", " WA", 47.6528732, -117.4334643, 0.706855093, -0.70735838),
                                                        new Location("32 W Pacific Avenue", " Spokane", " WA", 47.655975, -117.412994, 0.706905568, -0.707307937),
                                                        new Location("320 E Second Avenue", " Spokane", " WA", 47.6543192, -117.4055317, 0.706917999, -0.707295513),
                                                        new Location("4211 E Colbert Road", " Colbert", " WA", 47.8250374, -117.3495486, 0.707403286, -0.706810152),
                                                        new Location("4603 N Market", " Spokane", " WA", 47.7000354, -117.3653973, 0.707101853, -0.707111709),
                                                        new Location("544 E Providence Avenue", " Spokane", " WA", 47.692126, -117.4009041, 0.707008891, -0.707204658),
                                                        new Location("5508 N Alberta Street", " Spokane", " WA", 47.7084147, -117.4485016, 0.706941877, -0.707271647),
                                                        new Location("6607 N Havana", " Spokane", " WA", 47.7181876, -117.3476429, 0.707178733, -0.707034822),
                                                        new Location("6815 E Trent Avenue", " Spokane Valley", " WA", 47.6756953, -117.312726, 0.707162527, -0.707051031),
                                                        new Location("707 E Mission Avenue", " Spokane", " WA", 47.6721806, -117.3988472, 0.706970575, -0.707242961),
                                                        new Location("910 W Indiana Avenue", " Spokane", " WA", 47.6752872, -117.4262344, 0.706918591, -0.707294921),
                                                        new Location("9103 E Peone Road", " Mead", " WA", 47.775628, -117.281217, 0.707443891, -0.706769511),
                                                        new Location("919 E Trent Avenue", " Spokane", " WA", 47.6621613, -117.3955639, 0.706956143, -0.707257387),
                                                        new Location("9706 N Division Street", " Spokane", " WA", 47.7465452, -117.4107435, 0.707104345, -0.707109217),
                                                        new Location("9832 N Nevada", " Spokane", " WA", 47.7472565, -117.3936644, 0.707142429, -0.707071132),
                                                        new Location("9907 E Wellesley Avenue", " Spokane", " WA", 47.6997245, -117.2666531, 0.707312653, -0.70690085)};
                foreach (Location a in locArray)
                {
                    locations.Add(a);
                }
            }
            //initialize groups list
            {
                Group[] groupArr = new Group[41] {  new Group(locations[0],-1,28),
                                                    new Group(locations[1],-1,16), new Group(locations[2],-1,14),new Group(locations[3],-1,23),new Group(locations[4],-1,24),new Group(locations[5],-1,17),
                                                    new Group(locations[6],-1,27),new Group(locations[7],-1,13),new Group(locations[8],-1,17),new Group(locations[9],-1,21),new Group(locations[10],-1,24),
                                                    new Group(locations[11],-1,13),new Group(locations[12],-1,21),new Group(locations[13],-1,40),new Group(locations[14],-1,19),new Group(locations[15],-1,27),
                                                    new Group(locations[16],-1,20),new Group(locations[17],-1,25),new Group(locations[18],-1,22),new Group(locations[19],-1,27),new Group(locations[20],-1,12),
                                                    new Group(locations[21],-1,13),new Group(locations[22],-1,12),new Group(locations[23],-1,13),new Group(locations[24],-1,21),new Group(locations[25],-1,27),
                                                    new Group(locations[26],-1,12),new Group(locations[27],-1,27),new Group(locations[28],-1,22),new Group(locations[29],-1,24),new Group(locations[30],-1,19),
                                                    new Group(locations[31],-1,22),new Group(locations[32],-1,8),new Group(locations[33],-1,26),new Group(locations[34],-1,28),new Group(locations[35],-1,24),
                                                    new Group(locations[36],-1,22), new Group(locations[37],-1,28), new Group(locations[38],-1,23), new Group(locations[39],-1,14), new Group(locations[40],-1,12)};
                for (int i = 0; i < 41; i++)
                {
                    groups[i] = groupArr[i];
                }
            }
            //initialize buses list
            {
                Bus[] busArr = new Bus[30] { new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50),
                                                new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50),
                                                new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50),
                                                new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50),
                                                new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50), new Bus(50)};
                foreach (Bus b in busArr)
                {
                    buses.Add(b);
                }
            }
            //initialize dictionary
            {
                double[,] dict = new double[41, 41] { {     0,   25,  4,   22,  31,  22,  18,  26,  10,  24,  9,   16,  27,  19,  19,  24,  23,  22,  36,  19,  23,  17,  24,  20,  17,  21,  22,  25,  14,  18,  17,  12,  14,  27,  20,  17,  19,  23,  4,   6,   23,   },
                                                    {   25,  0,   22,  6,   15,  7,   8,   7,   26,  3,   27,  9,   5,   11,  9,   10,  4,   9,   20,  7,   11,  11,  1,   11,  17,  5,   4,   3,   30,  15,  11,  17,  20,  14,  8,   9,   31,  9,   21,  20,  19,   },
                                                    {   4,   22,  0,   21,  28,  21,  18,  23,  7,   22,  5,   16,  24,  18,  18,  24,  20,  19,  33,  16,  22,  16,  21,  19,  13,  20,  20,  22,  11,  15,  14,  11,  11,  23,  17,  16,  15,  20,  2,   3,   20,   },
                                                    {   22,  6,   21,  0,   14,  5,   8,   10,  25,  4,   24,  6,   9,   7,   7,   7,   6,   10,  19,  11,  8,   9,   4,   9,   18,  5,   7,   7,   31,  15,  13,  14,  20,  13,  10,  8,   31,  10,  20,  23,  18,   },
                                                    {   31,  15,  28,  14,  0,   16,  18,  14,  21,  13,  29,  17,  15,  17,  17,  18,  14,  15,  12,  18,  18,  19,  15,  19,  20,  13,  14,  13,  26,  18,  23,  24,  18,  8,   18,  18,  24,  19,  27,  24,  11,   },
                                                    {   22,  7,   21,  5,   16,  0,   5,   12,  25,  7,   22,  4,   9,   4,   4,   8,   7,   12,  20,  9,   5,   6,   5,   6,   18,  4,   7,   7,   28,  17,  12,  11,  22,  14,  10,  5,   32,  13,  17,  20,  19,   },
                                                    {   18,  8,   18,  8,   18,  5,   0,   11,  23,  7,   21,  4,   11,  4,   4,   11,  6,   8,   22,  5,   9,   4,   7,   6,   14,  8,   5,   7,   26,  12,  8,   11,  17,  15,  6,   3,   31,  8,   16,  17,  21,   },
                                                    {   26,  7,   23,  10,  14,  12,  11,  0,   21,  5,   27,  13,  6,   13,  11,  12,  6,   5,   20,  8,   14,  12,  6,   13,  14,  7,   6,   3,   25,  12,  12,  20,  16,  12,  7,   11,  30,  10,  22,  19,  19,   },
                                                    {   10,  26,  7,   25,  21,  25,  23,  21,  0,   24,  9,   23,  26,  25,  25,  29,  25,  18,  28,  19,  29,  22,  26,  26,  11,  24,  24,  24,  9,   12,  17,  18,  10,  18,  19,  22,  11,  22,  9,   6,   15,   },
                                                    {   24,  3,   22,  4,   13,  7,   7,   5,   24,  0,   26,  8,   4,   10,  9,   9,   3,   7,   18,  7,   11,  11,  4,   11,  16,  4,   4,   2,   27,  13,  11,  16,  18,  12,  9,   9,   30,  8,   20,  20,  17,   },
                                                    {   9,   27,  5,   24,  29,  22,  21,  27,  9,   26,  0,   19,  29,  21,  21,  27,  25,  23,  37,  21,  25,  19,  26,  23,  17,  24,  25,  27,  9,   18,  19,  14,  14,  26,  23,  19,  18,  26,  6,   9,   24,   },
                                                    {   16,  9,   16,  6,   17,  4,   4,   13,  23,  8,   19,  0,   10,  2,   2,   8,   8,   10,  21,  7,   6,   4,   6,   3,   16,  5,   8,   8,   26,  15,  10,  9,   20,  15,  8,   3,   31,  10,  15,  18,  20,   },
                                                    {   27,  5,   24,  9,   15,  9,   11,  6,   26,  4,   29,  10,  0,   15,  13,  14,  7,   9,   19,  11,  15,  15,  6,   15,  19,  9,   8,   5,   30,  16,  15,  20,  21,  13,  11,  14,  31,  12,  24,  24,  18,   },
                                                    {   19,  11,  18,  7,   17,  4,   4,   13,  25,  10,  21,  2,   15,  0,   1,   9,   9,   11,  22,  8,   8,   5,   7,   5,   18,  7,   9,   9,   27,  16,  12,  11,  21,  16,  9,   4,   32,  11,  16,  19,  21,   },
                                                    {   19,  9,   18,  7,   17,  4,   4,   11,  25,  9,   21,  2,   13,  1,   0,   9,   8,   10,  21,  7,   7,   5,   7,   4,   17,  5,   8,   9,   27,  15,  11,  10,  20,  15,  8,   4,   31,  10,  15,  18,  20,   },
                                                    {   24,  10,  24,  7,   18,  8,   11,  12,  29,  9,   27,  8,   14,  9,   9,   0,   9,   13,  22,  12,  8,   12,  7,   12,  21,  8,   9,   9,   34,  18,  16,  17,  23,  16,  13,  11,  34,  14,  23,  26,  21,   },
                                                    {   23,  4,   20,  6,   14,  7,   6,   6,   25,  3,   25,  8,   7,   9,   8,   9,   0,   8,   18,  5,   8,   9,   2,   9,   15,  3,   1,   3,   28,  14,  9,   15,  19,  11,  6,   7,   29,  7,   19,  19,  17,   },
                                                    {   22,  9,   19,  10,  15,  12,  8,   5,   18,  7,   23,  10,  9,   11,  10,  13,  8,   0,   20,  4,   13,  8,   10,  12,  10,  7,   8,   7,   21,  7,   7,   16,  12,  7,   3,   7,   26,  6,   18,  15,  14,   },
                                                    {   36,  20,  33,  19,  12,  20,  22,  20,  28,  18,  37,  21,  19,  22,  21,  22,  18,  20,  0,   22,  22,  23,  19,  23,  25,  17,  19,  17,  32,  22,  26,  29,  23,  15,  21,  22,  29,  23,  32,  30,  16,   },
                                                    {   19,  7,   16,  11,  18,  9,   5,   8,   19,  7,   21,  7,   11,  8,   7,   12,  5,   4,   22,  0,   13,  5,   7,   10,  10,  8,   6,   7,   23,  9,   5,   12,  13,  11,  3,   3,   28,  5,   15,  14,  16,   },
                                                    {   23,  11,  22,  8,   18,  5,   9,   14,  29,  11,  25,  6,   15,  8,   7,   8,   8,   13,  22,  13,  0,   9,   7,   9,   22,  6,   9,   9,   31,  19,  16,  14,  23,  16,  13,  8,   34,  14,  20,  23,  21,   },
                                                    {   17,  11,  16,  9,   19,  6,   4,   12,  22,  11,  19,  4,   15,  5,   5,   12,  9,   8,   23,  5,   9,   0,   12,  10,  14,  11,  10,  12,  26,  12,  8,   10,  17,  18,  9,   4,   31,  12,  15,  17,  21,   },
                                                    {   24,  1,   21,  4,   15,  5,   7,   6,   26,  4,   26,  6,   6,   7,   7,   7,   2,   10,  19,  7,   7,   12,  0,   10,  17,  4,   4,   4,   30,  16,  12,  16,  20,  13,  9,   9,   30,  9,   21,  21,  18,   },
                                                    {   20,  11,  19,  9,   19,  6,   6,   13,  26,  11,  23,  3,   15,  5,   4,   12,  9,   12,  23,  10,  9,   10,  10,  0,   19,  8,   11,  12,  29,  18,  13,  9,   23,  18,  11,  6,   34,  13,  18,  21,  23,   },
                                                    {   17,  17,  13,  18,  20,  18,  14,  14,  11,  16,  17,  16,  19,  18,  17,  21,  15,  10,  25,  10,  22,  14,  17,  19,  0,   15,  15,  15,  16,  3,   7,   13,  7,   12,  10,  13,  21,  13,  13,  10,  15,   },
                                                    {   21,  5,   20,  5,   13,  4,   8,   7,   24,  4,   24,  5,   9,   7,   5,   8,   3,   7,   17,  8,   6,   11,  4,   8,   15,  0,   7,   6,   28,  15,  12,  11,  20,  13,  10,  5,   31,  11,  17,  20,  18,   },
                                                    {   22,  4,   20,  7,   14,  7,   5,   6,   24,  4,   25,  8,   8,   9,   8,   9,   1,   8,   19,  6,   9,   10,  4,   11,  15,  7,   0,   4,   28,  14,  10,  14,  18,  11,  7,   7,   29,  7,   19,  19,  16,   },
                                                    {   25,  3,   22,  7,   13,  7,   7,   3,   24,  2,   27,  8,   5,   9,   9,   9,   3,   7,   17,  7,   9,   12,  4,   12,  15,  6,   4,   0,   28,  14,  11,  16,  18,  13,  8,   10,  31,  8,   21,  20,  18,   },
                                                    {   14,  30,  11,  31,  26,  28,  26,  25,  9,   27,  9,   26,  30,  27,  27,  34,  28,  21,  32,  23,  31,  26,  30,  29,  16,  28,  28,  28,  0,   16,  21,  21,  13,  24,  22,  26,  13,  27,  12,  10,  20,   },
                                                    {   18,  15,  15,  15,  18,  17,  12,  12,  12,  13,  18,  15,  16,  16,  15,  18,  14,  7,   22,  9,   19,  12,  16,  18,  3,   15,  14,  14,  16,  0,   8,   14,  5,   11,  9,   13,  19,  12,  14,  11,  13,   },
                                                    {   17,  11,  14,  13,  23,  12,  8,   12,  17,  11,  19,  10,  15,  12,  11,  16,  9,   7,   26,  5,   16,  8,   12,  13,  7,   12,  10,  11,  21,  8,   0,   11,  11,  15,  6,   8,   25,  9,   12,  11,  17,   },
                                                    {   12,  17,  11,  14,  24,  11,  11,  20,  18,  16,  14,  9,   20,  11,  10,  17,  15,  16,  29,  12,  14,  10,  16,  9,   13,  11,  14,  16,  21,  14,  11,  0,   15,  23,  14,  8,   26,  17,  10,  13,  25,   },
                                                    {   14,  20,  11,  20,  18,  22,  17,  16,  10,  18,  14,  20,  21,  21,  20,  23,  19,  12,  23,  13,  23,  17,  20,  23,  7,   20,  18,  18,  13,  5,   11,  15,  0,   15,  13,  17,  18,  16,  12,  9,   11,   },
                                                    {   27,  14,  23,  13,  8,   14,  15,  12,  18,  12,  26,  15,  13,  16,  15,  16,  11,  7,   15,  11,  16,  18,  13,  18,  12,  13,  11,  13,  24,  11,  15,  23,  15,  0,   10,  15,  22,  13,  23,  20,  9,    },
                                                    {   20,  8,   17,  10,  18,  10,  6,   7,   19,  9,   23,  8,   11,  9,   8,   13,  6,   3,   21,  3,   13,  9,   9,   11,  10,  10,  7,   8,   22,  9,   6,   14,  13,  10,  0,   5,   27,  5,   16,  14,  16,   },
                                                    {   17,  9,   16,  8,   18,  5,   3,   11,  22,  9,   19,  3,   14,  4,   4,   11,  7,   7,   22,  3,   8,   4,   9,   6,   13,  5,   7,   10,  26,  13,  8,   8,   17,  15,  5,   0,   30,  9,   14,  17,  20,   },
                                                    {   19,  31,  15,  31,  24,  32,  31,  30,  11,  30,  18,  31,  31,  32,  31,  34,  29,  26,  29,  28,  34,  31,  30,  34,  21,  31,  29,  31,  13,  19,  25,  26,  18,  22,  27,  30,  0,   31,  17,  15,  18,   },
                                                    {   23,  9,   20,  10,  19,  13,  8,   10,  22,  8,   26,  10,  12,  11,  10,  14,  7,   6,   23,  5,   14,  12,  9,   13,  13,  11,  7,   8,   27,  12,  9,   17,  16,  13,  5,   9,   31,  0,   18,  15,  16,   },
                                                    {   4,   21,  2,   20,  27,  17,  16,  22,  9,   20,  6,   15,  24,  16,  15,  23,  19,  18,  32,  15,  20,  15,  21,  18,  13,  17,  19,  21,  12,  14,  12,  10,  12,  23,  16,  14,  17,  18,  0,   4,   21,   },
                                                    {   6,   20,  3,   23,  24,  20,  17,  19,  6,   20,  9,   18,  24,  19,  18,  26,  19,  15,  30,  14,  23,  17,  21,  21,  10,  20,  19,  20,  10,  11,  11,  13,  9,   20,  14,  17,  15,  15,  4,   0,   19,   },
                                                    {   23,  19,  20,  18,  11,  19,  21,  19,  15,  17,  24,  20,  18,  21,  20,  21,  17,  14,  16,  16,  21,  21,  18,  23,  15,  18,  16,  18,  20,  13,  17,  25,  11,  9,   16,  20,  18,  16,  21,  19,  0,    } };
                for (int i = 0; i < 41; i++)
                {
                    for (int j = 0; j < 41; j++)
                    {
                        locations[i].distTo.Add(locations[j].address, dict[i, j]);
                    }
                }
            }
            //initialize cluster
            {
                for (int i = 0; i < 7; i++)
                {
                    Cluster newcluster = new Cluster();
                    clusters.Add(newcluster);
                }
            }
            int groupStudentSum = 0;
            foreach (Group g in groups)
            {
                groupStudentSum += g.numStudents;
            }
            /*Console.WriteLine("Total students: " + groupStudentSum);
            Console.ReadLine();*/
            Console.WriteLine("Beginning runkMeans()");
            runKMeans(ref groups);
            Console.WriteLine("Finished runKmeans()");
            Console.WriteLine("Starting kMeansToClusters()");
            kMeansToClusters();
            Console.WriteLine("Finished kMeansToClusters()");
            Console.WriteLine("Starting findLargeClusters()");
            findLargeClusters();
            Console.WriteLine("Finished findLargeClusters()");
            Console.WriteLine("Starting createCombinations() loop");
            /*int clusterStudentSum = 0;
            foreach (Group g in clusters[7].members)
            {
                clusterStudentSum += g.numStudents;
            }
            Console.WriteLine("Total students in first cluster (expected 230): " + clusterStudentSum);
            Console.ReadLine();*/
            for (int c = 0; c < clusters.Count(); c++)
            {
                Console.WriteLine("Starting createCombinations(" + c + ") of " + clusters.Count());
                Console.WriteLine("Size of clusters[" + c + "]: " + clusters[c].members.Count());
                createCombinations(c);
                Console.WriteLine("Finished createCombinations(" + c + ") of " + clusters.Count());
            }
            Console.WriteLine("Finished createCombinations() loop");
            Console.WriteLine("Starting fillBuses()");
            fillBuses();
            Console.WriteLine("Finished fillBuses()");
            Console.WriteLine("Starting moveOnes()");
            moveOnes();
            Console.WriteLine("Finished moveOnes()");
            outputBuses();

            int groupCounter = 0;
            for(int bus = 0; bus < buses.Count(); bus++)
            {
                groupCounter += buses[bus].groups.Count();
            }
            if (groupCounter < groups.Count())
            {
                validSolution = false;
                Console.WriteLine("Invalid solution");
            }

        }
    }
}
