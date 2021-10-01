<script language="javascript" type="text/javascript">
    function Barcode_Initialize(ctrl) {
        ctrl.element.addEventListener('keydown', function (e) {
            if (e.keyCode === 13) { //Enter key 
                e.preventDefault();
                e.stopPropagation();
            }
        });
    };
</script>