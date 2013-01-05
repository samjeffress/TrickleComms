
function ShowSendAll() {
    $('#TimeSeparatedDiv').hide(200);
    $('#SendAllByDiv').show(200);
    $('#TimeSeparatorSeconds').val('');
}

function ShowTimeSeparated() {
    $('#SendAllByDiv').hide(200);
    $('#TimeSeparatedDiv').show(200);
    $('#SendAllBy').val('');
}

function CheckSendAll() {
    if ($('#SendAllBy').val().length == 0) {
        $('#SendAllByDiv').hide();
    }
}

function CheckTimeSeparated() {
    if ($('#TimeSeparatorSeconds').val().length == 0) {
        $('#TimeSeparatedDiv').hide();
    }
}