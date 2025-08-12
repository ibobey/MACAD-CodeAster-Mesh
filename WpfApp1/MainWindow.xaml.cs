using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Macad.Interaction;
using Macad.Occt;
using Macad.Occt.Helper;

using CodeAsterMesh.src;
using System.IO;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>

namespace CodeAsterMesh
{

    public partial class MainWindow : Window
    {
        #region Constructors

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }
        #endregion Constructors

        #region Methods


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Keep a reference
            // OcctControl is initialized via XAML.
            // Its BuildWindowCore and InitializeOcct will be called automatically.
            // NewDrawMeshFunction();
            VisualizeMesh();
        }

        public void VisualizeMesh()
        {

            /* Give FilePath Manually*/
            string projectDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;
            string filePath = System.IO.Path.Combine(projectDir, "samples", "pontoon.mail");

            CodeAsterMeshViewer viwer = new CodeAsterMeshViewer(OcctControl, filePath);
            viwer.ImportMesh();
            viwer.VisualizeMesh();
            LoadMeshDataToGrid(viwer.Mesh);
        }

        private void LoadMeshDataToGrid(CodeAsterMeshBuilder mesh)
        {

            var data = new List<DataGridProperty>
                {
                    new DataGridProperty { Key = "File", Value = mesh.FilePath },
                    new DataGridProperty { Key = "Node", Value = mesh.Nodes.Count.ToString() },
                    new DataGridProperty { Key = "Quad", Value = mesh.Quads.Count.ToString() },
                    new DataGridProperty { Key = "Triangle", Value = mesh.Triangles.Count.ToString() },
                    new DataGridProperty { Key = "Beam", Value = mesh.Beams.Count.ToString() },
                    new DataGridProperty { Key = "Total", Value = mesh.ElementsUngrouped.Count.ToString(), },
                    new DataGridProperty { Key = "Groups", Value = mesh.Groups.Keys.ToList().Count.ToString(), },
                };

            myDataGrid.ItemsSource = data;
        }

        #endregion Methods

        #region Old Test Methods 
        public void OldMeshVisualizeFunction()
        {
            if (OcctControl == null) return;

            string file_path = @"C:\Users\ibrah\OneDrive\Masaüstü\obirler\send\Mesh_and_MeshViewer\pontoon.mail";
            string file_path_2 = @"D:\Mesh\OCCT-Code_Aster-Mesh-Viewer\Mesh_and_MeshViewer\Test_fullship12.mail";
            string file_path_3 = @"C:\Users\ibrah\OneDrive\Masaüstü\shell_beam_t.mail";

            var mesh = new CodeAsterMeshBuilder(file_path);
            mesh.PullDataFromMeshFile();
            mesh.GetlAllElements();

            // Simple COMPOUND of all faces
            var comp = new TopoDS_Compound();
            var builder = new BRep_Builder();
            builder.MakeCompound(comp);

            // Simple COMPOUND of all faces
            var comp2 = new TopoDS_Compound();
            var builder2 = new BRep_Builder();
            builder2.MakeCompound(comp2);


            foreach (var element_ in mesh.ElementsUngrouped)
            {

                if (element_.Count == 4)
                {

                    //DIVIDES QUAD ELEMENT TO 2 TRIANGLE ELEMENT
                    /*var mk = new BRepBuilderAPI_MakePolygon();
                    mk.Add(element_[0]); mk.Add(element_[1]); mk.Add(element_[2]);
                    mk.Close();
                    if (!mk.IsDone()) continue;

                    var face = new BRepBuilderAPI_MakeFace(mk.Wire(), true).Face();
                    builder.Add(comp, face);

                    var mk2 = new BRepBuilderAPI_MakePolygon();
                    mk2.Add(element_[0]); mk2.Add(element_[2]); mk2.Add(element_[3]);
                    mk2.Close();
                    if (!mk2.IsDone()) continue;

                    var face2 = new BRepBuilderAPI_MakeFace(mk2.Wire(), true).Face();
                    builder.Add(comp, face2);*/


                    var mk = new BRepBuilderAPI_MakePolygon();
                    mk.Add(element_[0]); mk.Add(element_[1]); mk.Add(element_[2]); mk.Add(element_[3]);
                    mk.Close();
                    if (!mk.IsDone()) continue;

                    var face = new BRepBuilderAPI_MakeFace(mk.Wire(), true).Face();
                    builder.Add(comp, face);
                }

                if (element_.Count == 3)
                {

                    var mk = new BRepBuilderAPI_MakePolygon();
                    mk.Add(element_[0]); mk.Add(element_[1]); mk.Add(element_[2]);
                    mk.Close();
                    if (!mk.IsDone()) continue;

                    var face = new BRepBuilderAPI_MakeFace(mk.Wire(), true).Face();
                    builder.Add(comp, face);
                }

                if (element_.Count == 2)
                {

                    var p1 = element_[0];
                    var p2 = element_[1];

                    // Build an edge from p1 to p2
                    var edgeMaker = new BRepBuilderAPI_MakeEdge(p1, p2);
                    TopoDS_Edge edge = edgeMaker.Edge();

                    builder2.Add(comp2, edge);
                    // Display the edge
                }

                else
                {
                    continue;
                }
            }
            OcctControl.DisplayShape(comp);
            OcctControl.DisplayShape(comp2);

            // OcctControl.FitAll(); // if available
            OcctControl.Update();

            // MessageBox.Show($"NODE: {mesh.Nodes.Count}\nQUAD4: {mesh.Quads.Count}\nTRIA3: {mesh.Triangles.Count}\nBeams: {mesh.Beams.Count}\n ");


            mesh.GetlAllElements();
            mesh.GroupElements();
            LoadMeshDataToGrid(mesh);
        }
        private void DisplaySampleBox()
        {
            if (OcctControl == null)
                return;

            try
            {
                var makeBox = new BRepPrimAPI_MakeBox(10, 20, 30);
                makeBox.Build();
                if (makeBox.IsDone())
                {
                    OcctControl.DisplayShape(makeBox.Shape());
                    OcctControl.Update(); // Adjust the view to fit the new shape
                }
                else
                {
                    MessageBox.Show("Failed to create OCCT box.", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating/displaying shape: {ex.Message}", "OCCT Error");
            }
        }
        #endregion
    }

    internal class DataGridProperty
    {
        // Sample Class For Property Tab
        public string Key { get; set; }
        public string Value { get; set; }
    }
}

