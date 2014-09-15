function getData() {
    $.get("/data", function (data) {
        $(".data").html(data);
        //alert("Load was performed.");
    });
}