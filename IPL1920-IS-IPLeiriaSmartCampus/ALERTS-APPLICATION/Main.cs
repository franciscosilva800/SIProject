﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Models;
using System.Xml;
using ALERTS_APPLICATION.Controller;
using System.Threading;

namespace ALERTS_APPLICATION
{
    public partial class Main : Form
    {
        private List<Alert> alerts;
        private List<GeneratedAlert> generatedAlerts;
        private List<Parameter> parameters;
        private ErrorProvider errorProvider;
        private List<ReadingType> readingTypes;
        private int userID;
        private int size = 0;
        private Thread t;
        private string[] conditions = { "<", ">", "=", "<>" };

        //TODO: SELECT ALERT AND EDIT AND SEE THE PARAMETERS
        //TODO: IMPROVE UI
        


        public Main()
        {
            InitializeComponent();
        }

        public void setReadingTypes(List<ReadingType> readingTypes)
        {
            this.readingTypes = readingTypes;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            Console.WriteLine("STARTING_MAIN_FORM");


            this.parameters = new List<Parameter>();
            this.alerts = new List<Alert>();

            lvAlerts.HideSelection = false;
            lvAlerts.FullRowSelect = true;

            lvGeneratedAlerts.HideSelection = false;
            lvGeneratedAlerts.FullRowSelect = true;

            /*READS SAVED ALERTS*/
            MQTTHandler.Instance.getReadingTypes();

            DateTime startTime = DateTime.Now;
            while (MQTTHandler.Instance.ReadingTypes == null && DateTime.Now.Subtract(startTime).Seconds <= 5)
            { }

           

            this.readingTypes = MQTTHandler.Instance.ReadingTypes;

            /*GETS USER ID*/
            this.userID = LoginController.Instance.checkUserLogin();

            

            cbReadingType.DataSource = this.readingTypes;
            cbReadingType.ValueMember = "MeasureName";

            cbParameterCondition.DataSource = this.conditions;
            nrParameterValue2.Visible = false;
            lblTo.Visible = false;

            cbParameterCondition.SelectedIndex = 0;

            lvParameters.View = View.Details;
            lvParameters.Columns.Add("Condition");
            lvParameters.Columns.Add("Data Type");
            lvParameters.Columns.Add("Value");

            lvAlerts.View = View.Details;
            lvAlerts.Columns.Add("ID");
            lvAlerts.Columns.Add("SensorID");
            lvAlerts.Columns.Add("Description");
            lvAlerts.Columns.Add("Creation Date");
            lvAlerts.Columns.Add("Enabled");
            lvAlerts.Columns.Add("Number Of Parameters");


            lvGeneratedAlerts.View = View.Details;
            lvGeneratedAlerts.Columns.Add("AlertID");
            lvGeneratedAlerts.Columns.Add("Alert Description");
            lvGeneratedAlerts.Columns.Add("Generated Timestamp");

            errorProvider = new ErrorProvider();

            errorProvider.SetIconAlignment(txtAlertDescription, ErrorIconAlignment.MiddleRight);
            errorProvider.SetIconPadding(txtAlertDescription, 3);
            errorProvider.SetIconAlignment(nrParameterValue, ErrorIconAlignment.MiddleRight);
            errorProvider.SetIconPadding(nrParameterValue, 3);
            errorProvider.SetIconAlignment(btnAddParameter, ErrorIconAlignment.MiddleRight);
            errorProvider.SetIconPadding(btnAddParameter, 3);



            errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.BlinkIfDifferentError;


            if (this.readingTypes == null)
            {
                errorProvider.SetError(cbReadingType, "Reading Type could not be read from Database");
            }

            /*CREATES THREAD TO CHECK FOR NEW GENERATED ALERTS*/
            t = new Thread(new ThreadStart(checkGeneratedAlerts));
            t.Start();

            /*CREATES THREAD TO CHECK FOR CHANGES IN ALERTS*/
            t = new Thread(new ThreadStart(checkAlertsChanges));
            t.Start();


            /*LOAD ALERTS TO LIST*/
            loadAlertsToList();

     
        }



        private void btnAdicionar_Click(object sender, EventArgs e)
        {
            if(cbReadingType.Items.Count <= 0)
            {

                errorProvider.SetError(cbReadingType, "Cannot insert parameter without reading type!");
                return;
            }

            string condition = cbParameterCondition.SelectedItem.ToString();

            ReadingType dataType = (ReadingType)cbReadingType.SelectedItem;



            string value = nrParameterValue.Value.ToString();
            string value2;
            string finalValue;

            if (condition.Equals("<>"))
            {
                value2 = nrParameterValue2.Value.ToString();
                finalValue = value + ":" + value2;
            }
            else
            {
                finalValue = value;
            }


            Parameter parameter = new Parameter
            {
                Condition = condition,
                ReadingType = (ReadingType)cbReadingType.SelectedItem,
                Value = finalValue
            };

            parameters.Add(parameter);

            ListViewItem item = null;



            lvParameters.Items.Clear();
            foreach (Parameter parameterI in parameters)
            {
                item = new ListViewItem(new string[] { parameterI.Condition,parameter.ReadingType.MeasureName, parameterI.Value.ToString() });
                lvParameters.Items.Add(item);
            }

            cbParameterCondition.SelectedIndex = 0;
            cbReadingType.SelectedIndex = 0;
            nrParameterValue.Value= 0;
            nrParameterValue2.Value = 0;
            

        }

        private void label2_Click_1(object sender, EventArgs e)
        {

        }

        private void btnCriarAlert_Click(object sender, EventArgs e)
        {
            errorProvider.SetError(btnAddParameter, "");
            errorProvider.SetError(txtAlertDescription, "");


            if (txtAlertDescription.Text.Length <= 0)
            {
                errorProvider.SetError(txtAlertDescription, "Description is mandatory");
                return;
            }

            if (parameters.Count <= 0)
            {
                errorProvider.SetError(btnAddParameter, "At least one parameter is neccessary to create the alert");
                return;
            }

            if(nrSensorID.Value <= 0)
            {
                errorProvider.SetError(nrSensorID, "Sensor ID must be positive integer");
                return;
            }


            /*CRIAR O ALERTA*/
            Alert alert = AlertController.Instance.create(txtAlertDescription.Text,Convert.ToInt32(nrSensorID.Value),this.parameters);

            saveAlert(alert);


            /*LIMPA OS DADOS*/
            txtAlertDescription.Clear();
            nrParameterValue.Value = 0;
            nrSensorID.Value = 0;

            // cbReadingType.SelectedIndex = 0;
            cbParameterCondition.SelectedIndex = 0;

            lvParameters.Items.Clear();

            parameters.Clear();

        }


        private void saveAlert(Alert alert)
        {
            if (alert == null)
            {
                return;
            }

            AlertController.Instance.save(alert);

            loadAlertsToList();
          
        }

        public void checkGeneratedAlerts()
        { 
            while (true)
            {
                if(AlertController.Instance.generatedAlerts.Count > size)
                {
                    this.generatedAlerts = AlertController.Instance.generatedAlerts;
                    size = this.generatedAlerts.Count;

                    /*DELEGATES UI RESPONSIBLE THREAD TO UPDATE*/
                    this.Invoke((MethodInvoker)delegate
                    {
                        loadGeneratedAlertsToList();
                    });
                }

            }
        }

        public void checkAlertsChanges()
        {
            while (true)
            {
                if (AlertController.Instance.alerts.Count > size)
                {
                    this.alerts = AlertController.Instance.alerts;
                    size = this.alerts.Count;

                    /*DELEGATES UI RESPONSIBLE THREAD TO UPDATE*/
                    this.Invoke((MethodInvoker)delegate
                    {
                        loadAlertsToList();
                    });
                }

            }
        }

        private void lvParameters_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void btnLimparAlerts_Click(object sender, EventArgs e)
        {
            alerts.Clear();
            lvAlerts.Items.Clear();

            /*APAGA O FICHEIRO QUE GUARDA OS ALERTAS*/
            AlertController.Instance.clean();
        }

        private void loadAlertsToList()
        {
            lvAlerts.Items.Clear();

            /*LOADS ALERTS DATA*/
            ListViewItem item = null;

            this.alerts = AlertController.Instance.getAllAlerts();


            /*CARREGA O ALERTAS NA LISTA*/
            foreach (Alert alertI in alerts)
            {
                item = new ListViewItem(new string[] {alertI.Id.ToString(),alertI.SensorID.ToString() ,alertI.Description, alertI.CreatedAt, alertI.Enabled.ToString(), alertI.Parameters.Count.ToString() });
                lvAlerts.Items.Add(item);
            }
        }

        private void loadGeneratedAlertsToList()
        {
            lvGeneratedAlerts.Items.Clear();

            /*LOADS ALERTS DATA*/
            ListViewItem item = null;

            Alert alert = null;

            /*CARREGA O ALERTAS NA LISTA*/
            foreach (GeneratedAlert generatedAlert in this.generatedAlerts)
            {
                alert = AlertController.Instance.getAlert(generatedAlert.alert_id);

                item = new ListViewItem(new string[] {alert.Id.ToString(),alert.Description,generatedAlert.timestamp });
                lvGeneratedAlerts.Items.Add(item);

            }
        }

        private void btnDisableAlert_Click(object sender, EventArgs e)
        {
            if(lvAlerts.SelectedIndices.Count > 0)
            {
                ListViewItem item = lvAlerts.SelectedItems[0];
                AlertController.Instance.disableAlert(int.Parse(item.SubItems[0].Text));
                loadAlertsToList();
            }
        }

        private void addAlertTab_Click(object sender, EventArgs e)
        {

        }

        private void cbParameterCondition_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbParameterCondition.SelectedItem.Equals("<>")){
                nrParameterValue2.Visible = true;
                lblTo.Visible = true;

            }
            else
            {
                nrParameterValue2.Visible = false;
                lblTo.Visible = false;
            }
        }

        private void label2_Click_2(object sender, EventArgs e)
        {

        }

        private void nrParameterValue2_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}