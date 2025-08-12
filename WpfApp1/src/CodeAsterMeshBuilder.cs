using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Macad.Occt;

namespace CodeAsterMesh.src
{
    public class CodeAsterMeshBuilder
    {

        #region Fields

        private string filePath;
        private List<Pnt> nodes = new List<Pnt>();
        private List<List<Pnt>> quads = new List<List<Pnt>>();
        private List<List<Pnt>> triangles = new List<List<Pnt>>();
        private List<List<Pnt>> beams = new List<List<Pnt>>();
        private List<List<Pnt>>? elementsUngrouped;
        private Dictionary<string, List<int>> groups = new Dictionary<string, List<int>>();
        #endregion

        #region Properties

        public string FilePath { get { return filePath; } set { filePath = value; } }
        public List<Pnt> Nodes { get { return this.nodes; } private set { } }
        public List<List<Pnt>>? Quads { get { return this.quads; } private set { } }
        public List<List<Pnt>>? Triangles { get { return this.triangles; } private set { } }
        public List<List<Pnt>>? Beams { get { return this.beams; } private set { } }

        public List<List<Pnt>> ElementsUngrouped
        {
            get
            {
                if (this.elementsUngrouped == null)
                {
                    PullDataFromMeshFile();
                    GetlAllElements();
                }
                return this.elementsUngrouped;
            }
            private set { }
        }

        public Dictionary<string, List<int>> Groups
        {
            get
            {
                return this.groups;
            }
            private set { }
        }
        #endregion

        #region Contructors

        public CodeAsterMeshBuilder(string filePath)
        {
            this.filePath = filePath;
            IsFileExists();
        }
        #endregion


        #region Private Methods
        private bool IsFileExists()
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file '{filePath}' was not found.", filePath);
            }
            return true;
        }

        private IEnumerable<string> ExtractDataBetweenKeywords(string startKeyword, string endKeyword)
        {
            using var reader = new StreamReader(filePath, Encoding.ASCII);
            bool capture = false;

            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains(startKeyword, StringComparison.Ordinal))
                {
                    capture = true;
                    continue;
                }

                if (capture && line.Contains(endKeyword, StringComparison.Ordinal))
                {
                    capture = false;
                    continue;
                }

                if (capture)
                {
                    yield return line.Trim();
                }
            }
        }

        private List<double> GetNode(string coords)
        {
            coords = coords.Replace("N", "");
            var values = coords
                .Split((char[])null!, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => double.Parse(x, CultureInfo.InvariantCulture))
                .ToList();
            return values;
        }

        private List<int> GetElement(string nodes)
        {
            nodes = nodes.Replace("N", "");
            nodes = nodes.Replace("E", "");
            var values = nodes
                .Split((char[])null!, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.Parse(x, CultureInfo.InvariantCulture))
                .ToList();
            return values;
        }
        #endregion

        #region Public Methods
        public void PullDataFromMeshFile()
        {
            // Pull All COOR_3D (Points)
            foreach (var data in ExtractDataBetweenKeywords(KeyWords.COOR_3D, KeyWords.FINSF))
            {
                List<double> nodeTemp = GetNode(data);

                double P1 = nodeTemp[1];
                double P2 = nodeTemp[2];
                double P3 = nodeTemp[3];

                Pnt pnt = new Pnt(P1, P2, P3);
                this.nodes.Add(pnt);
            }

            // Pull All QUAD4 (Quad Elements)
            foreach (var data in ExtractDataBetweenKeywords(KeyWords.QUAD4, KeyWords.FINSF))
            {
                List<int> quadTemp = GetElement(data);

                int N1 = quadTemp[1];
                int N2 = quadTemp[2];
                int N3 = quadTemp[3];
                int N4 = quadTemp[4];

                List<Pnt> quadsTemp = new List<Pnt>()
                {
                    this.nodes[N1],this.nodes[N2],this.nodes[N3],this.nodes[N4],
                };

                quads.Add(quadsTemp);
            }

            // Pull All TRIA3 (Triangle Elements)
            foreach (var data in ExtractDataBetweenKeywords(KeyWords.TRIA3, KeyWords.FINSF))
            {
                List<int> triangleTemp = GetElement(data);

                int N1 = triangleTemp[1];
                int N2 = triangleTemp[2];
                int N3 = triangleTemp[3];

                List<Pnt> trianglesTemp = new List<Pnt>()
                {
                    this.nodes[N1],this.nodes[N2],this.nodes[N3],
                };

                triangles.Add(trianglesTemp);
            }

            // Get All SEG2 (Beams)
            foreach (var data in ExtractDataBetweenKeywords(KeyWords.SEG2, KeyWords.FINSF))
            {
                List<int> beamTemp = GetElement(data);

                int N1 = beamTemp[1];
                int N2 = beamTemp[2];

                List<Pnt> beamsTemp = new List<Pnt>()
                {
                    this.nodes[N1],this.nodes[N2],
                };
                beams.Add(beamsTemp);
            }

        }

        public void GetlAllElements()
        {
            this.elementsUngrouped = this.quads.Concat(this.triangles).Concat(this.beams).ToList();
        }

        public void GroupElements()
        {
            if (this.elementsUngrouped.Count == 0) { throw new Exception("There is no pulled mesh ! Ex: Pull data from Mesh file First!"); }

            List<string>? groupNames = File.ReadAllLines(this.filePath)
                                  .Where(line => line.Contains(KeyWords.GROUP_MA_NOM))
                                  .ToList();
            foreach (var group in groupNames)
            {
                List<int> indexTemps = new List<int>();

                foreach (var subGroup in ExtractDataBetweenKeywords(startKeyword: group, endKeyword: KeyWords.FINSF))
                {
                    var digits = new string(subGroup.Where(char.IsDigit).ToArray()); // "N4  " -> "4"
                    indexTemps.Add(int.Parse(digits));
                }
                this.groups[group] = indexTemps;
            }
        }
        #endregion
    }
}

