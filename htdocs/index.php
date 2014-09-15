<html>
<head>
<?php
// Create connection
$con=mysqli_connect("127.0.0.1","root","","hackatlon") or die(mysql_error());;

// Check connection
if (mysqli_connect_errno()) {
  echo "Failed to connect to MySQL: " . mysqli_connect_error();
}
?>

<script type="text/javascript" src="http://ajax.googleapis.com/ajax/libs/jquery/1.8.2/jquery.min.js"></script>

<script src="http://code.highcharts.com/highcharts.js"></script>
<script src="http://code.highcharts.com/modules/exporting.js"></script>

<script src="http://code.highcharts.com/maps/modules/map.js"></script>
<script src="http://code.highcharts.com/maps/modules/data.js"></script>


<script type="text/javascript">
$(function () {
    $(document).ready(function () {
        Highcharts.setOptions({
            global: {
                useUTC: false
            }
        });

        $('#spline_auto').highcharts({
            chart: {
                type: 'spline',
                animation: Highcharts.svg, // don't animate in old IE
                marginRight: 10,
                events: {
                    load: function () {

                        // set up the updating of the chart each second
                        var series = this.series[0];
                        setInterval(function () {
                            var x = (new Date()).getTime(), // current time
                                y = Math.random();
                            series.addPoint([x, y], true, true);
                        }, 1000);
                    }
                }
            },
            title: {
                text: 'Live data'
            },
            xAxis: {
                type: 'datetime',
                tickPixelInterval: 150
            },
            yAxis: {
                title: {
                    text: 'Value'
                },
                plotLines: [{
                    value: 0,
                    width: 1,
                    color: '#808080'
                }]
            },
            tooltip: {
                formatter: function () {
                    return '<b>' + this.series.name + '</b><br/>' +
                        Highcharts.dateFormat('%Y-%m-%d %H:%M:%S', this.x) + '<br/>' +
                        Highcharts.numberFormat(this.y, 2);
                }
            },
            legend: {
                enabled: false
            },
            exporting: {
                enabled: false
            },
            series: [{
                name: 'Random data',
                data: (function () {
                    // generate an array of random data
                    var data = [],
                        time = (new Date()).getTime(),
                        i;

                    for (i = -19; i <= 0; i += 1) {
                        data.push({
                            x: time + i * 1000,
                            y: Math.random()
                        });
                    }
                    return data;
                }())
            }]
        });
    });
});


</script>

<script>
$(function () {

    $('#heat_map').highcharts({

        data: {
            csv: document.getElementById('csv').innerHTML
        },

        chart: {
            type: 'heatmap',
            inverted: true
        },


        title: {
            text: 'Heat map study',
            align: 'left'
        },

      /*  subtitle: {
            text: 'Position variation by day and hour through April 2013',
            align: 'left'
        },*/

        xAxis: {
            tickPixelInterval: 50,
           /* min: Date.UTC(2013, 3, 1),
            max: Date.UTC(2013, 3, 30)*/
        },

        yAxis: {
            title: {
                text: null
            },
            labels: {
                format: '{value}:00'
            },
            minPadding: 0,
            maxPadding: 0,
            startOnTick: false,
            endOnTick: false,
            tickPositions: [0, 6, 12, 18, 24],
            tickWidth: 1,
            min: 0,
            max: 23
        },

        colorAxis: {
            stops: [
                [0, '#3060cf'],
                [0.5, '#fffbbc'],
                [0.9, '#c4463a']
            ],
            min: -5
        },

        series: [{
            borderWidth: 0,
            colsize: 24 * 36e5, // one day
            tooltip: {
                headerFormat: 'Temperature<br/>',
                pointFormat: '{point.x:%e %b, %Y} {point.y}:00: <b>{point.value} ?</b>'
            }
        }]

    });
});

</script>

<title>Kinect Data Analysis</title>
<link href="style.css" rel="stylesheet" type="text/css">
</head>
<body>
<div class="container_top">
<h1>Today's status</h1>
<div class="top_left">
<table >
 <thead>
  <tr>
     <th>Number of shoppers</th>
  </tr>
 </thead>
<tr><td>207</td></tr>
</table>

<table>
<thead>
  <tr>
     <th>Number of pickups</th>
  </tr>
 </thead>
<tr><td><?php $result = mysqli_query($con,"SELECT NUM FROM pickup_v") or die(mysql_error());

while($row = mysqli_fetch_array($result)) {
  echo $row['NUM'] ;
  
}?></td></tr>
</table>


<table>
<thead>
  <tr>
     <th>Avg visit time</th>
  </tr>
 </thead>

<tr><td>141s</td></tr>
</table>

<table>
<thead>
  <tr>
     <th>Bounce rate</th>
  </tr>
 </thead>
<tr><td>74%</td></tr>
</table>

<table>
<thead>
  <tr>
     <th>Conversion rate</th>
  </tr>
 </thead>
<tr><td>6%</td></tr>
</table>
</div>
<div class="top_right">

<div id="heat_map" style="height: 400px; max-width: 400px; margin: 0 auto"></div>
<?php 
$result1 = mysqli_query($con,"SELECT * FROM polsi_group_v");

while($row1 = mysqli_fetch_array($result1)) {
  $PDXX= $row1['polso_dx_x'] ;
  $PDXY= $row1['polso_dx_y'] ;
  $PSXX= $row1['polso_sx_x'] ;
  $PSXY= $row1['polso_sx_y'] ;
  $NUM= $row1['NUM'] ;
 
}
?>


</div >
</div>
<div class="container_bottom">
<h1>Analytics</h1>
<div id="spline_auto" style="min-width: 310px; height: 400px; margin: 0 auto"></div>
</div>
<?php mysqli_close($con); ?>
</body>
</html>