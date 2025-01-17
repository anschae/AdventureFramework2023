window.mouse = {
    objRef: null,
    absMousePos: { x: 0, y: 0 },
    svgMousePos: { x: 0, y: 0 },

    updateDelay: 100,

    timeout: false,

    init: function (objRef, disableContextMenu) {
        window.mouse.objRef = objRef;
        window.addEventListener("mousedown", (event) => {
            objRef.invokeMethodAsync("MouseDown", event.button);
        });
        window.addEventListener("mouseup", (event) => {
            objRef.invokeMethodAsync("MouseUp", event.button);
        });
        if (disableContextMenu) {
            window.addEventListener("contextmenu", (event) => {
                event.preventDefault();
            });
        }
        window.addEventListener("mousemove", (event) => {
            window.mouse.absMousePos = { x: event.clientX, y: event.clientY };
            if (!window.mouse.timeout) {
                window.mouse.timeout = true;
                setTimeout(() => {
                    window.mouse.svgMousePos = convertToSvgCoords2(
                        window.mouse.absMousePos.x,
                        window.mouse.absMousePos.y
                    );
                    // console.log(window.mouse.svgMousePos.x, window.mouse.svgMousePos.y);
                    objRef.invokeMethodAsync(
                        "MouseMove",
                        window.mouse.svgMousePos.x,
                        window.mouse.svgMousePos.y
                    );
                    window.mouse.timeout = false;
                }, window.mouse.updateDelay);
            }
        });
    },

    setDelay: function (delay) {
        window.mouse.updateDelay = delay;
    },
};

// const container = document.getElementById("container");
// let containerRect = container.getBoundingClientRect();

// window.addEventListener("resize", () => {
//     containerRect = container.getBoundingClientRect();
// });

// // convert mouse px viewport coords to svg coords
// function convertToSvgCoords(x, y) {
//     screenWidth = window.innerWidth;
//     screenHeight = window.innerHeight;

//     // first, convert to px coords relative to container
//     // then, scale that in the same way the svg is scaled
//     return {
//         x: (x - containerRect.x) * (1920 / containerRect.width),
//         y: (y - containerRect.y) * (1080 / containerRect.height),
//     };
// }
function convertToSvgCoords2(x, y) {
    containerRect = document
        .getElementById("container")
        .getBoundingClientRect();
    // first, convert to px coords relative to container
    // then, scale that in the same way the svg is scaled
    return {
        x: Math.round((x - containerRect.x) * (1920 / containerRect.width)),
        y: Math.round((y - containerRect.y) * (1080 / containerRect.height)),
    };
}
