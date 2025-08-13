using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Macad.Occt;

namespace CodeAsterMesh.src
{
    public class CodeAsterMeshViewer
    {
        #region Fields

        private string filePath;
        private CodeAsterMeshBuilder mesh;
        private OcctHwndControl? occtControl;

        #endregion

        #region Properties

        public string FilePath
        {
            get { return this.filePath; }
            set { this.filePath = value; }
        }


        public CodeAsterMeshBuilder Mesh
        {
            get { return this.mesh; }
            private set { }
        }

        public OcctHwndControl OcctControl
        {
            get
            {
                if (occtControl == null)
                { throw new InvalidOperationException("There is no Occt Controller Active"); }
                return this.occtControl;
            }
            set
            { this.occtControl = value; }
        }
        #endregion Properties

        #region Constructors

        public CodeAsterMeshViewer(OcctHwndControl occtControl, string filePath)
        {
            this.occtControl = occtControl;
            this.filePath = filePath;
        }

        #endregion


        #region Methods
        public void ImportMesh()
        {
            this.mesh = new CodeAsterMeshBuilder(filePath: this.filePath);
            this.mesh.PullDataFromMeshFile();
            this.mesh.GetlAllElements();
            this.mesh.GroupElements();
        }

        public void VisualizeMesh()
        {
            if (this.occtControl == null) { throw new Exception("There is no OcctController assigned!"); }
            if (this.mesh == null) { throw new Exception("There is no mesh data calculated"); }

            // Simple COMPOUND of all QUAD and TRIA3
            var comp = new TopoDS_Compound();
            var builder = new BRep_Builder();
            builder.MakeCompound(comp);

            // Simple COMPOUND for SEG2 (Beams)
            var comp2 = new TopoDS_Compound();
            var builder2 = new BRep_Builder();
            builder2.MakeCompound(comp2);

            foreach (var element_ in this.mesh.ElementsUngrouped)
            {
                if (element_.Count == 4)
                {
                    var mk = new BRepBuilderAPI_MakePolygon();
                    mk.Add(element_[0]); mk.Add(element_[1]); mk.Add(element_[2]); mk.Add(element_[3]);
                    mk.Close();
                    if (!mk.IsDone()) continue;

                    TopoDS_Wire wire = mk.Wire();

                    var fp = new BRepBuilderAPI_FindPlane(wire);

                    if (fp.Found()) // If plane is planar create face
                    {
                        var face = new BRepBuilderAPI_MakeFace(wire, true).Face();
                        builder.Add(comp, face);
                    }
                    
                    else // Divide quad to 2 triangle 
                    {
                        var triangle1 = new BRepBuilderAPI_MakePolygon();
                        triangle1.Add(element_[0]); triangle1.Add(element_[1]); triangle1.Add(element_[2]);
                        triangle1.Close();
                        if (!triangle1.IsDone()) continue;

                        var face = new BRepBuilderAPI_MakeFace(triangle1.Wire(), true).Face();
                        builder.Add(comp, face);

                        var triangle2 = new BRepBuilderAPI_MakePolygon();
                        triangle2.Add(element_[0]); triangle2.Add(element_[2]); triangle2.Add(element_[3]);
                        triangle2.Close();
                        if (!triangle2.IsDone()) continue;

                        var face2 = new BRepBuilderAPI_MakeFace(triangle2.Wire(), true).Face();
                        builder.Add(comp, face2);
                    }
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

            this.occtControl.DisplayShape(comp);
            this.occtControl.DisplayShape(comp2);
            this.occtControl.Update();
        }

        #endregion
    }
}
