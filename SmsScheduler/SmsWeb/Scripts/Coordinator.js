
function ShowSendAll() {
    $('#TimeSeparatedDiv').hide(200);
    $('#SendAllByDiv').show(200);
    $('#TimeSeparatorSeconds').val('');
    $('#SendAllAtOnce').val(false);
}

function ShowTimeSeparated() {
    $('#SendAllByDiv').hide(200);
    $('#TimeSeparatedDiv').show(200);
    $('#SendAllBy').val('');
    $('#SendAllAtOnce').val(false);
}

function HideTimingOptions() {
    $('#SendAllByDiv').hide(200);
    $('#TimeSeparatedDiv').hide(200);
    $('#SendAllBy').val('');
    $('#SendAllAtOnce').val(true);
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