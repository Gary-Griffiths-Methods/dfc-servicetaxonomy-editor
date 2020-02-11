﻿using Microsoft.AspNetCore.Mvc;

namespace DFC.ServiceTaxonomy.GraphVisualiser.Controllers
{
    //todo: wwwroot files need to be in this module
    public class VisualiseController : Controller
    {
        public ActionResult Data([FromQuery]string fetch)
        {
            //return Json();
            return Content(@"{
  ""_comment"": ""Empty ontology for WebVOWL Editor [Additional Information added by WebVOWL Exporter Version: 1.1.7]"",
  ""header"": {
    ""languages"": [
      ""en""
    ],
    ""baseIris"": [
      ""http://www.w3.org/2000/01/rdf-schema""
    ],
    ""iri"": ""https://nationalcareers.service.gov.uk/test"",
    ""title"": ""test"",
    ""description"": {
      ""en"": ""New ontology description""
    }
  },
  ""namespace"": [],
  ""settings"": {
    ""global"": {
      ""zoom"": ""2.09"",
      ""translation"": [
        -1087.15,
        -750.73
      ],
      ""paused"": false
    },
    ""gravity"": {
      ""classDistance"": 200,
      ""datatypeDistance"": 120
    },
    ""filter"": {
      ""checkBox"": [
        {
          ""id"": ""datatypeFilterCheckbox"",
          ""checked"": false
        },
        {
          ""id"": ""objectPropertyFilterCheckbox"",
          ""checked"": false
        },
        {
          ""id"": ""subclassFilterCheckbox"",
          ""checked"": false
        },
        {
          ""id"": ""disjointFilterCheckbox"",
          ""checked"": true
        },
        {
          ""id"": ""setoperatorFilterCheckbox"",
          ""checked"": false
        }
      ],
      ""degreeSliderValue"": ""0""
    },
    ""modes"": {
      ""checkBox"": [
        {
          ""id"": ""pickandpinModuleCheckbox"",
          ""checked"": false
        },
        {
          ""id"": ""nodescalingModuleCheckbox"",
          ""checked"": true
        },
        {
          ""id"": ""compactnotationModuleCheckbox"",
          ""checked"": false
        },
        {
          ""id"": ""colorexternalsModuleCheckbox"",
          ""checked"": true
        }
      ],
      ""colorSwitchState"": false
    }
  },
  ""class"": [
    {
      ""id"": ""Class1"",
      ""type"": ""owl:Class""
    },
    {
      ""id"": ""Class2"",
      ""type"": ""owl:Class""
    }
  ],
  ""classAttribute"": [
    {
      ""id"": ""Class1"",
      ""iri"": ""https://nationalcareers.service.gov.uk/testJobProfile"",
      ""baseIri"": ""http://visualdataweb.org/newOntology/"",
      ""label"": ""JobProfile"",
        ""pos"": [
            864.79,
            688.54
        ]
    },
    {
      ""id"": ""Class2"",
      ""iri"": ""https://nationalcareers.service.gov.uk/testOccupation"",
      ""baseIri"": ""https://nationalcareers.service.gov.uk/test"",
      ""label"": ""Occupation""
    }
  ],
  ""property"": [
    {
      ""id"": ""objectProperty1"",
      ""type"": ""owl:ObjectProperty""
    }
  ],
  ""propertyAttribute"": [
    {
      ""id"": ""objectProperty1"",
      ""iri"": ""https://nationalcareers.service.gov.uk/testhasOccupation"",
      ""baseIri"": ""https://nationalcareers.service.gov.uk/test"",
      ""label"": ""hasOccupation"",
      ""attributes"": [
        ""object""
      ],
      ""domain"": ""Class1"",
      ""range"": ""Class2""
    }
  ]
}
", "application/json");        }
    }
}
