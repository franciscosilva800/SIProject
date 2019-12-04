﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataShowApplication {
    public partial class InfoHumiditySensor : UserControl, ISensorView<HumiditySensorData> {
        public InfoHumiditySensor() {
            InitializeComponent();
        }

        public void update(HumiditySensorData data) {
            lblInfoSensor.Text = data.sensor;
            lblInfoHumidity.Text = data.humidity.ToString();
            lblInfoBaterry.Text = data.baterry.ToString();
            lblInfoDate.Text = data.date.ToString();
        }
    }
}
