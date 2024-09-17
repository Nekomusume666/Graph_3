namespace TEST_GRAPH
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;
    using Microsoft.VisualBasic;
    using System.Drawing.Drawing2D;

    public partial class Form1 : Form
    {
        private List<Vertex> vertices = new List<Vertex>();
        private List<Edge> edges = new List<Edge>();
        private Vertex selectedVertex = null;
        private bool firstClick = true;
        private int vertexCounter = 1; 
        private Vertex previousSelectedVertex = null;
        private List<Edge> highlightedEdges = new List<Edge>();


        public Form1()
        {
            InitializeComponent();


            panelGraph.MouseDown += PanelGraph_MouseDown;
            panelGraph.MouseUp += PanelGraph_MouseUp;
            panelGraph.MouseMove += PanelGraph_MouseMove;
            panelGraph.Paint += PanelGraph_Paint;
            InitContextMenu();
        }

        private void DgvIncidenceMatrix_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string rowVertexId = dgvIncidenceMatrix.Rows[e.RowIndex].HeaderCell.Value.ToString();
            string colVertexId = dgvIncidenceMatrix.Columns[e.ColumnIndex].HeaderText;

            if (int.TryParse(dgvIncidenceMatrix.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out int newWeight))
            {
                var vertexRow = vertices.FirstOrDefault(v => v.Id == rowVertexId);
                var vertexCol = vertices.FirstOrDefault(v => v.Id == colVertexId);

                if (vertexRow == null || vertexCol == null) return;

                var edge = edges.FirstOrDefault(ed => ed.Start == vertexRow && ed.End == vertexCol);

                if (newWeight != 0)
                {
                    if (edge != null)
                    {
                        edge.Weight = newWeight;
                    }
                    else
                    {
                        edges.Add(new Edge(vertexRow, vertexCol, newWeight));
                    }

                    //dgvIncidenceMatrix.Rows[e.ColumnIndex].Cells[e.RowIndex].Value = newWeight;

                }
                else
                {
                    if (edge != null)
                    {
                        edges.Remove(edge);
                    }

                    //dgvIncidenceMatrix.Rows[e.ColumnIndex].Cells[e.RowIndex].Value = 0;
                }

                panelGraph.Invalidate();
            }
        }


        // ���������� ������� ���������
        private void UpdateIncidenceMatrix()
        {
            dgvIncidenceMatrix.Rows.Clear();
            dgvIncidenceMatrix.Columns.Clear();

            foreach (var vertex in vertices)
            {
                dgvIncidenceMatrix.Columns.Add(vertex.Id, vertex.Id);
            }

            foreach (var vertexRow in vertices)
            {
                var row = new DataGridViewRow();
                row.HeaderCell.Value = vertexRow.Id;

                foreach (var vertexCol in vertices)
                {
                    var edge = edges.FirstOrDefault(ed => ed.Start == vertexRow && ed.End == vertexCol);
                    int cellValue = edge != null ? edge.Weight : 0;

                    var cell = new DataGridViewTextBoxCell { Value = cellValue };
                    row.Cells.Add(cell);
                }

                dgvIncidenceMatrix.Rows.Add(row);
            }

            dgvIncidenceMatrix.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;

            dgvIncidenceMatrix.CellValueChanged += DgvIncidenceMatrix_CellValueChanged;
        }

        // ������������� ������������ ����
        private void InitContextMenu()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("�������� �������", null, (s, e) => AddVertex(MousePosition));
            contextMenu.Items.Add("������� �������", null, (s, e) => RemoveVertex());
            contextMenu.Items.Add("�������� ����", null, (s, e) => AddEdge());
            contextMenu.Items.Add("�������� ��� ����", null, (s, e) => ChangeEdgeWeight());
            contextMenu.Items.Add("������� ����", null, (s, e) => RemoveEdge());
            contextMenu.Items.Add("����� ���������� ���� (��������)", null, (s, e) => FindShortestPath());
            contextMenu.Items.Add("����� ���������� ���� (�����)", null, (s, e) => FindShortestPathFloyd());
            contextMenu.Items.Add("�������� ����� ���������� (�������� � �����)", null, (s, e) => CompareAlgorithms());

            panelGraph.ContextMenuStrip = contextMenu;
        }


        // ���������� �������
        private void AddVertex(Point position)
        {
            Point localPoint = panelGraph.PointToClient(position);
            vertices.Add(new Vertex(localPoint, "X" + vertexCounter++));
            panelGraph.Invalidate();
            UpdateIncidenceMatrix();

        }

        // ����� ��� ������ ��������� ������
        private void ResetSelectedVertices()
        {
            previousSelectedVertex = null;
            selectedVertex = null;
            panelGraph.Invalidate(); 
            UpdateIncidenceMatrix();
        }

        private void FindShortestPathFloyd()
        {
            if (previousSelectedVertex == null || selectedVertex == null)
            {
                MessageBox.Show("�� ������� ��������� � �������� �������!");
                return;
            }

            int n = vertices.Count;
            int[,] graph = new int[n, n];
            int[,] distances;
            int[,] predecessors;

            // ������������� ������� ����� �����
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    graph[i, j] = int.MaxValue;
                    if (i == j)
                    {
                        graph[i, j] = 0;
                    }
                }
            }

            foreach (var edge in edges)
            {
                int startIndex = vertices.IndexOf(edge.Start);
                int endIndex = vertices.IndexOf(edge.End);
                graph[startIndex, endIndex] = edge.Weight;
            }

            var startTime = DateTime.Now;
            FloydWarshall.ComputeShortestPaths(graph, out distances, out predecessors);
            var endTime = DateTime.Now;

            int startIdx = vertices.IndexOf(previousSelectedVertex);
            int endIdx = vertices.IndexOf(selectedVertex);

            if (distances[startIdx, endIdx] != int.MaxValue)
            {
                string pathStr = "";
                var path = new List<Vertex>();
                PrintPath(predecessors, startIdx, endIdx, ref pathStr, path);

                // ��������� ������ highlightedEdges
                highlightedEdges.Clear();
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var startVertex = path[i];
                    var endVertex = path[i + 1];

                    var edge = edges.FirstOrDefault(e => e.Start == startVertex && e.End == endVertex);
                    if (edge != null)
                    {
                        highlightedEdges.Add(edge);
                    }
                }

                MessageBox.Show($"���������� ���� (�����) �� {previousSelectedVertex.Id} �� {selectedVertex.Id}: {pathStr}\n����� ����: {distances[startIdx, endIdx]}\n����� ����������: {endTime - startTime}");
            }
            else
            {
                MessageBox.Show("���� �� ����������.");
            }

            panelGraph.Invalidate(); // �������������� ���� � ����������� ������
            ResetSelectedVertices();
        }


        private void CompareAlgorithms()
        {
            if (previousSelectedVertex == null || selectedVertex == null)
            {
                MessageBox.Show("�� ������� ��������� � �������� �������!");
                return;
            }

            // ��������
            var startTimeDijkstra = DateTime.Now;
            var (distanceDijkstra, pathDijkstra) = Dijkstra(previousSelectedVertex, selectedVertex);
            var endTimeDijkstra = DateTime.Now;

            var pathStrDijkstra = string.Join(" -> ", pathDijkstra.Select(v => v.Id));

            // �����
            int n = vertices.Count;
            int[,] graph = new int[n, n];
            int[,] distances;
            int[,] predecessors;

            // ������������� ������� ����� �����
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    graph[i, j] = int.MaxValue;
                    if (i == j)
                    {
                        graph[i, j] = 0;
                    }
                }
            }

            foreach (var edge in edges)
            {
                int startIndex = vertices.IndexOf(edge.Start);
                int endIndex = vertices.IndexOf(edge.End);
                graph[startIndex, endIndex] = edge.Weight;
            }

            var startTimeFloyd = DateTime.Now;
            FloydWarshall.ComputeShortestPaths(graph, out distances, out predecessors);
            var endTimeFloyd = DateTime.Now;

            int startIdx = vertices.IndexOf(previousSelectedVertex);
            int endIdx = vertices.IndexOf(selectedVertex);

            string pathStrFloyd = "";
            var pathFloyd = new List<Vertex>();
            if (distances[startIdx, endIdx] != int.MaxValue)
            {
                PrintPath(predecessors, startIdx, endIdx, ref pathStrFloyd, pathFloyd);

                // ��������� ������ highlightedEdges
                highlightedEdges.Clear();
                for (int i = 0; i < pathFloyd.Count - 1; i++)
                {
                    var startVertex = pathFloyd[i];
                    var endVertex = pathFloyd[i + 1];

                    var edge = edges.FirstOrDefault(e => e.Start == startVertex && e.End == endVertex);
                    if (edge != null)
                    {
                        highlightedEdges.Add(edge);
                    }
                }
            }

            var message = $"����� ��������:\n���������� ����: {pathStrDijkstra}\n����� ����: {distanceDijkstra}\n����� ����������: {endTimeDijkstra - startTimeDijkstra}\n\n" +
                          $"����� ������:\n���������� ����: {pathStrFloyd}\n����� ����: {distances[startIdx, endIdx]}\n����� ����������: {endTimeFloyd - startTimeFloyd}";

            MessageBox.Show(message);

            panelGraph.Invalidate(); // �������������� ���� � ����������� ������
            ResetSelectedVertices();
        }


        private void PrintPath(int[,] predecessors, int start, int end, ref string pathStr, List<Vertex> path)
        {
            if (start == end)
            {
                path.Add(vertices[start]);
                pathStr = vertices[start].Id;
            }
            else if (predecessors[start, end] == -1)
            {
                path.Add(vertices[start]);
                path.Add(vertices[end]);
                pathStr = vertices[start].Id + " -> " + vertices[end].Id;
            }
            else
            {
                PrintPath(predecessors, start, predecessors[start, end], ref pathStr, path);
                pathStr += " -> " + vertices[end].Id;
                path.Add(vertices[end]);
            }
        }




        // ������ ������ ������ ������ � ����������� ����
        private void FindShortestPath()
        {
            if (previousSelectedVertex == null || selectedVertex == null)
            {
                MessageBox.Show("�� ������� ��������� � �������� �������!");
                return;
            }

            var startTime = DateTime.Now;
            var (distance, path) = Dijkstra(previousSelectedVertex, selectedVertex);
            var endTime = DateTime.Now;
            
            if (distance == int.MaxValue)
            {
                MessageBox.Show("���� �� ����������.");
            }
            else
            {
                string pathStr = string.Join(" -> ", path.Select(v => v.Id));
                MessageBox.Show($"���������� ���� (��������) �� {previousSelectedVertex.Id} �� {selectedVertex.Id}: {pathStr}" +
                    $"\n����� ����: {distance}\n����� ����������: {endTime - startTime}");

                highlightedEdges.Clear(); 
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var startVertex = path[i];
                    var endVertex = path[i + 1];

                    var edge = edges.FirstOrDefault(e => e.Start == startVertex && e.End == endVertex);
                    if (edge != null)
                    {
                        highlightedEdges.Add(edge);
                    }
                }

                panelGraph.Invalidate(); 
            }

            ResetSelectedVertices();
        }

        // �������� ��������
        private (int, List<Vertex>) Dijkstra(Vertex start, Vertex end)
        {
            var distances = new Dictionary<Vertex, int>();
            var previous = new Dictionary<Vertex, Vertex>();
            var unvisited = new List<Vertex>(vertices); 

            foreach (var vertex in vertices)
            {
                distances[vertex] = int.MaxValue;
                previous[vertex] = null;
            }
            distances[start] = 0;

            while (unvisited.Count > 0)
            {
                Vertex current = null;
                foreach (var vertex in unvisited)
                {
                    if (current == null || distances[vertex] < distances[current])
                    {
                        current = vertex;
                    }
                }

                if (current == end) break;  // ���� �������� �������� �������

                unvisited.Remove(current);

                foreach (var edge in edges)
                {
                    if (edge.Start == current)
                    {
                        int alt = distances[current] + edge.Weight;
                        if (alt < distances[edge.End])
                        {
                            distances[edge.End] = alt;
                            previous[edge.End] = current;
                        }
                    }
                }
            }

            if (distances[end] == int.MaxValue)
            {
                return (int.MaxValue, new List<Vertex>());
            }

            List<Vertex> path = new List<Vertex>();
            Vertex step = end;
            while (step != null)
            {
                path.Insert(0, step);
                step = previous[step];
            }

            return (distances[end], path);
        }

        // ��������� ������� ������� ���� �� �����
        private void PanelGraph_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                highlightedEdges.Clear();
                return;
            } 

            foreach (var vertex in vertices)
            {
                if (vertex.Contains(e.Location))
                {
                    if (firstClick) 
                    {
                        previousSelectedVertex = null;
                        selectedVertex = vertex;       
                        firstClick = false;           
                    }
                    else 
                    {
                        previousSelectedVertex = selectedVertex;
                        selectedVertex = vertex;                       
                    }
                    panelGraph.Invalidate();
                    return;
                }
            }

            // ���� �� ���� ������� �� �������, ���������� ���
            firstClick = true;
            previousSelectedVertex = null;
            selectedVertex = null;
            panelGraph.Invalidate();
        }

        private void PanelGraph_MouseUp(object sender, MouseEventArgs e)
        {
        }

        private void PanelGraph_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectedVertex != null && e.Button == MouseButtons.Left)
            {
                selectedVertex.Position = e.Location;
                panelGraph.Invalidate();
            }
        }

        private void AddEdge()
        {
            // ���������, ������� �� ��������� � �������� �������
            if (previousSelectedVertex == null || selectedVertex == null)
            {
                MessageBox.Show("����������, �������� ��������� � �������� �������.");
                return;
            }

            // ������� ���� ���� ����
            string input = Interaction.InputBox("������� ��� ����", "��� ����", "1");

            if (int.TryParse(input, out int weight))
            {
                // ���������, ���������� �� ���� �� ���������� � ��������� �������
                var existingEdge = edges.FirstOrDefault(edge => edge.Start == previousSelectedVertex && edge.End == selectedVertex);

                if (existingEdge == null)
                {
                    // ���� ���� ���, ��������� ����� ����
                    edges.Add(new Edge(previousSelectedVertex, selectedVertex, weight));
                }
                else
                {
                    // ���� ���� ��� ����������, ��������� � ���
                    existingEdge.Weight = weight;
                }

                // ��������� �������� � ������� ��������� ��� ����������� previousSelectedVertex -> selectedVertex
                dgvIncidenceMatrix.Rows[vertices.IndexOf(previousSelectedVertex)].Cells[vertices.IndexOf(selectedVertex)].Value = weight;

                // �������������� ����
                ResetSelectedVertices();
                panelGraph.Invalidate();
            }
            else
            {
                MessageBox.Show("������������ ���� ���� ����. ����������, ������� ����� �����.");
            }
        }

        // ����� ��� ��������� �����
        private void PanelGraph_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var edge in edges)
            {
                if (highlightedEdges.Contains(edge))
                {
                    edge.Draw(g, edges, edge.Weight, Color.Red);
                }
                else
                {
                    edge.Draw(g, edges, edge.Weight);
                }
            }

            foreach (var vertex in vertices)
            {
                if (vertex == previousSelectedVertex)
                {
                    vertex.Draw(g, Color.Green);
                }
                else if (vertex == selectedVertex)
                {
                    vertex.Draw(g, Color.Red);
                }
                else
                {
                    vertex.Draw(g);
                }
                g.DrawString(vertex.Id, SystemFonts.DefaultFont, Brushes.Black, vertex.Position.X - 10, vertex.Position.Y - 10);
            }
        }



        // �������� ��������� �������
        private void RemoveVertex()
        {
            if (selectedVertex == null)
            {
                MessageBox.Show("�������� ������� ��� ��������.");
                return;
            }

            edges.RemoveAll(edge => edge.Start == selectedVertex || edge.End == selectedVertex);

            vertices.Remove(selectedVertex);

            ResetSelectedVertices();
            UpdateIncidenceMatrix();
        }

        // �������� ���� ����� ���������� ���������
        private void RemoveEdge()
        {
            if (previousSelectedVertex == null || selectedVertex == null)
            {
                MessageBox.Show("����������, �������� ��������� � �������� ������� ��� �������� ����.");
                return;
            }

            var edgeToRemove = edges.FirstOrDefault(edge => edge.Start == previousSelectedVertex && edge.End == selectedVertex);

            if (edgeToRemove != null)
            {
                edges.Remove(edgeToRemove);
                MessageBox.Show("���� �������.");
                UpdateIncidenceMatrix();
            }
            else
            {
                MessageBox.Show("���� ����� ����� ��������� �� �������.");
            }

            ResetSelectedVertices();
        }


        // ��������� ���� ����
        private void ChangeEdgeWeight()
        {
            if (previousSelectedVertex == null || selectedVertex == null)
            {
                MessageBox.Show("����������, �������� ��������� � �������� ������� ��� ��������� ���� ����.");
                return;
            }

            var edgeToChange = edges.FirstOrDefault(edge => edge.Start == previousSelectedVertex && edge.End == selectedVertex);

            if (edgeToChange != null)
            {
                string input = Interaction.InputBox("������� ����� ��� ����", "��������� ���� ����", edgeToChange.Weight.ToString());

                if (int.TryParse(input, out int newWeight))
                {
                    edgeToChange.Weight = newWeight;
                    MessageBox.Show("��� ���� �������.");
                    UpdateIncidenceMatrix();
                }
                else
                {
                    MessageBox.Show("������������ ���� ����.");
                }
            }
            else
            {
                MessageBox.Show("���� ����� ����� ��������� �� �������.");
            }

            ResetSelectedVertices();
        }


    }
}
