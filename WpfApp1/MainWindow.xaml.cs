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
using System.Windows.Forms;
using System.Diagnostics;
using System;

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
            VisualizeMesh();
            // DrawManuelQuadElements();
        }

        public void VisualizeMesh()
        {
            /* Give FilePath Manually*/
            string filePathNew = @"C:\Users\ibrah\OneDrive\Masaüstü\v6-parca.mail";
            string filePathOld = @"C:\Users\ibrah\OneDrive\Masaüstü\shell_beam_t.mail";

            CodeAsterMeshViewer viwer = new CodeAsterMeshViewer(OcctControl, filePathNew);
            viwer.ImportMesh();
            viwer.VisualizeMeshNew();

            
            LoadMeshDataToGrid(viwer);
        }

        private void LoadMeshDataToGrid(CodeAsterMeshViewer viewer)
        {

            var data = new List<DataGridProperty>
                {
                    new DataGridProperty { Key = "File", Value = viewer.Mesh.FilePath },
                    new DataGridProperty { Key = "Node", Value = viewer.Mesh.Nodes.Count.ToString() },
                    new DataGridProperty { Key = "Quad", Value = viewer.Mesh.Quads.Count.ToString() },
                    new DataGridProperty { Key = "Triangle", Value = viewer.Mesh.Triangles.Count.ToString() },
                    new DataGridProperty { Key = "Beam", Value = viewer.Mesh.Beams.Count.ToString() },
                    new DataGridProperty { Key = "Total", Value = viewer.Mesh.ElementsUngrouped.Count.ToString(), },
                    new DataGridProperty { Key = "Groups", Value = viewer.Mesh.Groups.Keys.ToList().Count.ToString(), },
                    new DataGridProperty { Key = "Non Planar Quad Elements", Value = viewer.NonPlanarElements.ToString(), },
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
            string filePath = @"C:\Users\ibrah\OneDrive\Masaüstü\v6-parca.mail";
            string filePathNew = @"C:\Users\ibrah\OneDrive\Masaüstü\v6-parca.mail";

            var mesh = new CodeAsterMeshBuilder(filePathNew);
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

            foreach (var element_ in mesh.Quads)
            {

                if (element_.Count == 4)
                {

                    //DIVIDES QUAD ELEMENT TO 2 TRIANGLE ELEMENT

                    var mk = new BRepBuilderAPI_MakePolygon();
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
                    builder.Add(comp, face2);


                    /*
                    var mk = new BRepBuilderAPI_MakePolygon();
                    mk.Add(element_[0]); mk.Add(element_[1]); mk.Add(element_[2]); mk.Add(element_[3]);
                    mk.Close();
                    if (!mk.IsDone()) continue;

                    var wire = mk.Wire();
                    var fp = new BRepBuilderAPI_FindPlane(wire);
                    if (!fp.Found()) continue;

                    var face = new BRepBuilderAPI_MakeFace(mk.Wire(), true).Face();

                    builder.Add(comp, face);
                    */
                }

                if (element_.Count == 3)
                {
                    continue;
                    var mk = new BRepBuilderAPI_MakePolygon();
                    mk.Add(element_[0]); mk.Add(element_[1]); mk.Add(element_[2]);
                    mk.Close();
                    if (!mk.IsDone()) continue;

                    var face = new BRepBuilderAPI_MakeFace(mk.Wire(), true).Face();
                    builder.Add(comp, face);
                }

                if (element_.Count == 2)
                {
                    continue;
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
            // mesh.GroupElements();
            // LoadMeshDataToGrid(mesh);
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
                    return;
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }
        #endregion

        #region Manuel Mesh Drawing Functions

        public void DrawManuelRectangles()
        {
            /*
             	E0 N0 N1 N2 N3
	            E1 N3 N2 N4 N5
	            E2 N1 N0 N6 N7
	            E3 N7 N6 N8 N9
	            E4 N10 N11 N12 N13
	            E5 N13 N12 N14 N15
	            E6 N15 N14 N16 N17
	            E7 N17 N16 N18 N19
	            E8 N19 N18 N20 N21
            */

            Pnt N0 = new Pnt(118, -1, 3.975);
            Pnt N1 = new Pnt(118, -1, 3.75);
            Pnt N2 = new Pnt(118.22558, -1, 3.97442);
            Pnt N3 = new Pnt(118.45, -1, 4.2);
            Pnt N4 = new Pnt(118.225, -1, 4.2);
            Pnt N5 = new Pnt(114, -7, 0.3);
            Pnt N6 = new Pnt(114.28071, -7, 0.30969);
            Pnt N7 = new Pnt(114.18389, -7, 0.49254);
            Pnt N8 = new Pnt(117.175, -7, 4.01455);
            Pnt N9 = new Pnt(117.4, -7, 3.78955);
            Pnt N10 = new Pnt(117.4, -7, 4.01455);
            Pnt N11 = new Pnt(117.175, -7, 4.23955);
            Pnt N12 = new Pnt(116.95, -7, 4.23955);
            Pnt N13 = new Pnt(118.225, -7, 0.84955);
            Pnt N14 = new Pnt(118, -7, 1.07455);
            Pnt N15 = new Pnt(118, -7, 0.84955);
            Pnt N16 = new Pnt(118.225, -7, 0.62455);
            Pnt N17 = new Pnt(118.45, -7, 0.62455);
            Pnt N18 = new Pnt(117.4, -7, 0.84955);
            Pnt N19 = new Pnt(117.4, -7, 1.07455);
            Pnt N20 = new Pnt(117.175, -7, 0.84955);
            Pnt N21 = new Pnt(116.95, -7, 0.62455);

            List<Pnt> points = new List<Pnt>() { N0, N1, N2, N3, N4, N5, N6, N7, N8, N9, N10, N11, N12, N13, N14, N15, N16, N17, N18, N19, N20, N21 };

            // N1..N4: Pnt / gp_Pnt
            var poly = new BRepBuilderAPI_MakePolygon();
            poly.Add(new Pnt(0, 0, 0)); poly.Add(new Pnt(5, 5, 0)); poly.Add(new Pnt(10, 0, 0)); poly.Add(new Pnt(15, 5, 0)); ;
            poly.Close();
            if (!poly.IsDone()) { return; } 

            TopoDS_Wire w = poly.Wire();
            var mkFace = new BRepBuilderAPI_MakeFace(w, /*OnlyPlane*/ true); // düzlem bulamazsa NotDone olur
            if (!mkFace.IsDone()) return; 
            TopoDS_Face face = mkFace.Face();

            OcctControl.DisplayShape(poly.Wire());


            OcctControl.DisplayShape(face);


            // OcctControl.FitAll();
            OcctControl.Update();


        }

        public void DrawManuelQuadElements()
        {
            Pnt P1 = new Pnt(114, -2.75, 4.89234);
            Pnt P2 = new Pnt(114, -3, 4.8911);
            Pnt P3 = new Pnt(114.25965, -3.07198, 4.89075);
            Pnt P4 = new Pnt(114.2537, -2.74499, 4.89236);

            var fill = new BRepOffsetAPI_MakeFilling();

            // Toleransları ayarla (mm çalışanlar için tipik, örnek):
            double tol2d = 1e-5;
            double tol3d = 1e-3;          // “bu kadar yaklaşsın” (işine göre 1e-4…1e-3)
            double tolAng = 0.01;          // ≈0.57°  (radyan)
            double tolCurv = 0.1;

            fill.SetConstrParam(tol2d, tol3d, tolAng, tolCurv);

            TopoDS_Edge e12 = new BRepBuilderAPI_MakeEdge(P1, P2).Edge();
            TopoDS_Edge e23 = new BRepBuilderAPI_MakeEdge(P2, P3).Edge();
            TopoDS_Edge e34 = new BRepBuilderAPI_MakeEdge(P3, P4).Edge();
            TopoDS_Edge e41 = new BRepBuilderAPI_MakeEdge(P4, P1).Edge();

            bool isBound = true;
            fill.Add(e12, GeomAbs_Shape.C0, isBound);
            fill.Add(e23, GeomAbs_Shape.C0, isBound);
            fill.Add(e34, GeomAbs_Shape.C0, isBound);
            fill.Add(e41, GeomAbs_Shape.C0, isBound);

            fill.Build();
            if (!fill.IsDone())
                throw new InvalidOperationException("Filling çözülemedi.");

            TopoDS_Shape faceShape = fill.Shape();
            OcctControl.DisplayShape(faceShape);


            // OcctControl.FitAll();
            OcctControl.Update();
        }

        public void DrawViaAIS()
        {

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

