- support either 1 or more FAQItemContent content items or 1 FAQGroupContents
  - the marketer can select which data source they would prefer to use
- expand all / collapse all UI can be displayed or hidden through widget
  properties
- use the design.html template as an example of multiple FAQs being displayed
- ensure multiple instances of this widget can be placed on the same page
  - the HTML IDs / attributes used to hook in the javascript functionality need
    to be unique for each widget instance (use a content item GUID)
- don't use an IIFE for the js - instead use ES Modules in a `<script>` block
