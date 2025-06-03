using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinanceTool
{
    public partial class uc_clusteringPopup : UserControl
    {

        DataTable originDataTable;
        DataTable transformDataTable;

        public uc_clusteringPopup()
        {
            InitializeComponent();
        }

        public void initUI()
        {
            
            
            //1.데이터 불러오기
            originDataTable = DataHandler.firstClusteringData;

            Debug.WriteLine("originDataTable 데이터 확인");
            Debug.WriteLine($"originDataTable Table count :{originDataTable.Rows.Count}");


            transformDataTable = DataHandler.secondClusteringData;

            Debug.WriteLine("transformDataTable 데이터 확인");
            Debug.WriteLine($"transformDataTable Table count :{transformDataTable.Rows.Count}");

            //2.DataGridView 설정
            DataHandler.SetupDataGridView(dataGridView1, originDataTable);

            Debug.WriteLine($"uc_clusteringPopup -. SetupDataGridView -. dataGridView1 완료");
            DataHandler.SetupDataGridView(dataGridView2, transformDataTable);

            Debug.WriteLine($"uc_clusteringPopup -. SetupDataGridView -. dataGridView2 완료");
        }

       
       
    }
}
