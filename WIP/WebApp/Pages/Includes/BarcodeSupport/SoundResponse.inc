<script language="javascript" type="text/javascript">
    function ActionCallback(callbackContext) {
        var baseUrl = (location.href.indexOf("HideScript") > 0) ? "../../Sounds/" : "../../../Sounds/";
        var edInfoMessageSoundFile = px_alls["edInfoMessageSoundFile"];

        if ((callbackContext.info.name.toLowerCase().startsWith("scan") || callbackContext.info.name == "ElapsedTime") && callbackContext.control.longRunInProcess == null && edInfoMessageSoundFile != null) {
            var soundFile = edInfoMessageSoundFile.getValue();
            if (soundFile != null && soundFile != "") {
                var audio = new Audio(baseUrl + soundFile + '.wav');
                audio.play();
            }
        }
    };

    window.addEventListener('load', function () { px_callback.addHandler(ActionCallback); });
</script>