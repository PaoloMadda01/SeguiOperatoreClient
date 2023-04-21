////<script src="https://cdn.jsdelivr.net/npm/chart.js@2.9.3/dist/Chart.min.js"></script>
//    <style>
//        canvas {
//            width: 50%;
//            height: auto;
//        }
//    </style>
//</head >
//    <body>
//        <canvas id="myChart"></canvas>
//        <script>
//
//            var chartData = [
//            {DateRecording: '@Model.chartData[0].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[0].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[1].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[1].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[2].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[2].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[3].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[3].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[4].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[4].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[5].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[5].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[6].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[6].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[7].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[7].NumberOfAccount.ToString()},
//          {DateRecording: '@Model.chartData[8].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[8].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[9].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[9].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[10].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[10].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[11].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[11].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[12].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[12].NumberOfAccount.ToString()},
//            {DateRecording: '@Model.chartData[13].DateRecording.ToShortDateString()', NumberOfAccount: @Model.chartData[13].NumberOfAccount.ToString()}
//            ];
//
//            var labels = chartData.map(function (data) {
//            return data.DateRecording;
//        });
//
//            var data = chartData.map(function (data) {
//            return data.NumberOfAccount;
//        });
//
//            var ctx = document.getElementById('myChart').getContext('2d');
//            var chart = new Chart(ctx, {
//                type: 'line',
//            data: {
//                labels: labels,
//            datasets: [{
//                label: "Number of account",
//            data: data,
//            backgroundColor: 'rgba(255, 99, 132, 0.2)',
//            borderColor: 'rgba(255, 99, 132, 1)',
//            borderWidth: 1
//                }]
//            },
//            options: {
//                scales: {
//                yAxes: [{
//                ticks: {
//                beginAtZero: true
//                        }
//                    }]
//                }
//            }
//        });