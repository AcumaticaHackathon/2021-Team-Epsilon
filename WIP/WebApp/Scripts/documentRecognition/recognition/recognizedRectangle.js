'use strict';

const rectClass = 'recognition-rect';
const mappedRectClass = 'mapped';

function RecognizedRectangle(unit, pageWidth, pageHeight, coordinates, containerWidth, containerHeight) {
    this.xRelative = null;
    this.yRelative = null;
    this.widthRelative = null;
    this.heightRelative = null;
    this.element = null;

    this._initElement(unit, pageWidth, pageHeight, coordinates, containerWidth, containerHeight);
    this.markAsNotMapped();
}

RecognizedRectangle.prototype.equals = function (otherRect) {
    return this.xRelative === otherRect.xRelative &&
        this.yRelative === otherRect.yRelative &&
        this.widthRelative === otherRect.widthRelative &&
        this.heightRelative === otherRect.heightRelative;
}

RecognizedRectangle.prototype._initElement = function (unit, pageWidth, pageHeight, coordinates, containerWidth, containerHeight) {
    this.element = document.createElement("div");
    this.element.classList.add(rectClass);

    let localCoordinates = JSON.parse(JSON.stringify(coordinates));

    if (unit && unit !== 'scale' && pageWidth && pageHeight) {
        localCoordinates.forEach(function (coord) {
            coord.x /= pageWidth;
            coord.y /= pageHeight;
        });
    }

    const xMin = Math.min(localCoordinates[0].x, localCoordinates[1].x, localCoordinates[2].x, localCoordinates[3].x);
    const yMin = Math.min(localCoordinates[0].y, localCoordinates[1].y, localCoordinates[2].y, localCoordinates[3].y);
    const xMax = Math.max(localCoordinates[0].x, localCoordinates[1].x, localCoordinates[2].x, localCoordinates[3].x);
    const yMax = Math.max(localCoordinates[0].y, localCoordinates[1].y, localCoordinates[2].y, localCoordinates[3].y);

    this.xRelative = xMin;
    this.yRelative = yMin;

    this.widthRelative = xMax - xMin;
    this.heightRelative = yMax - yMin;

    this.rescale(containerWidth, containerHeight);
}

RecognizedRectangle.prototype.rescale = function (containerWidth, containerHeight) {
    this.element.style.left = this.convertRelativeToPixel(this.xRelative, containerWidth) + 'px';
    this.element.style.top = this.convertRelativeToPixel(this.yRelative, containerHeight) + 'px';
    this.element.style.width = this.convertRelativeToPixel(this.widthRelative, containerWidth) + 'px';
    this.element.style.height = this.convertRelativeToPixel(this.heightRelative, containerHeight) + 'px';
}

RecognizedRectangle.prototype.convertRelativeToPixel = function (relativeValue, valueOf100Percents) {
    const pixelsFloat = valueOf100Percents * relativeValue;
    const pixelsInt = pixelsFloat.toFixed(0);

    return pixelsInt;
}

RecognizedRectangle.prototype.subscribeOnMousedown = function (callback) {
    const that = this;

    this.element.addEventListener('mousedown', function (event) {
        callback(that, event);
    });
}

RecognizedRectangle.prototype.appendToParent = function (parent) {
    parent.appendChild(this.element);
}

RecognizedRectangle.prototype.isMapped = function () {
    return this.element.classList.contains(mappedRectClass) ? true : false;
}

RecognizedRectangle.prototype.markAsMapped = function () {
    this.element.classList.add(mappedRectClass);
}

RecognizedRectangle.prototype.markAsNotMapped = function () {
    this.element.classList.remove(mappedRectClass);
}
