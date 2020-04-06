﻿function debouncePreview(func, wait, immediate) {
    var timeout;
    return function () {
        var context = this, args = arguments;
        var later = function () {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };
        var callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow) func.apply(context, args);
    };
};
function initVueMultiselectPreview(element) {
    // only run script if element exists
    if (element) {
        var elementId = element.id;
        var selectedItems = JSON.parse(element.dataset.selectedItems || "[]");
        var searchUrl = element.dataset.searchUrl;
        var multiple = JSON.parse(element.dataset.multiple);

        var debouncedSearch = debouncePreview(function (vm, query) {
            vm.isLoading = true;
            var searchFullUrl = searchUrl;
            if (query) {
                searchFullUrl += '&query=' + query;
            }
            fetch(searchFullUrl).then(function (res) {
                res.json().then(function (json) {
                    vm.options = json;
                    vm.isLoading = false;
                })
            });
        }, 250);

        var vueMultiselect = Vue.component('vue-multiselect', window.VueMultiselect.default);

        var vm = new Vue({
            el: '#' + elementId,
            components: { 'vue-multiselect': vueMultiselect },
            data: {
                value: null,
                arrayOfItems: selectedItems,
                options: [],
            },
            computed: {
                selectedIds: function () {
                    return this.arrayOfItems.map(function (x) { return x.id }).join(',');
                },
                isDisabled: function () {
                    return this.arrayOfItems.length > 0 && !multiple;
                }
            },
            watch: {
                selectedIds: function () {
                    // We add a delay to allow for the <input> to get the actual value
                    // before the form is submitted
                    setTimeout(function () { $(document).trigger('contentpreview:render') }, 100);
                }
            },
            created: function () {
                var self = this;
                self.asyncFind();
                if (self.arrayOfItems.length === 1) {
                    self.showPreview(self.arrayOfItems[0].id);
                }
            },
            methods: {
                asyncFind: function (query) {
                    var self = this;
                    debouncedSearch(self, query);
                },
                showPreview(contentItemId) {
                    // todo use fetch then
                    // we could use the observed selectedIds, but we'd have to calc if something was added or deleted
                    // just just onSelect instead?
                    //todo: only show preview if only single item pickable
                    $.ajax({
                        url : '/Contents/ContentItems/' + contentItemId,
                        type: 'GET',

                        success: function(data){
                            $('#previewhere').html(data);
                            $('#preview-collapse').collapse('show');
                        }
                    });
                },
                onSelect: function (selectedOption, id) {
                    var self = this;

                    for (i = 0; i < self.arrayOfItems.length; i++) {
                        if (self.arrayOfItems[i].id === selectedOption.id) {
                            return;
                        }
                    }

                    self.arrayOfItems.push(selectedOption);
                },
                onInput: function (value, id) {
                    this.showPreview(value.id);
                },
                remove: function (item) {
                    this.arrayOfItems.splice(this.arrayOfItems.indexOf(item), 1)
                    $('#preview-collapse').collapse('hide');
                }
            }
        })

        /*Hook for other scripts that might want to have access to the view model*/
        // var event = new CustomEvent("vue-multiselect-created", { detail: { vm: vm } });
        // document.querySelector("body").dispatchEvent(event);
    }
}