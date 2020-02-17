﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.Neo4j.Services;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;

// https://localhost:5001/index.html?visualise=theid

//todo:
// add a custom part that selects the visualisation rule/heuristic
// e.g. visualizing a job profile, might want to follow all relationship paths until reach another job profile (follow to self)
// in addition would want to whitelist any esco namespace nodes to occupation and skill (would you want to show skills off occupation when displaying job profile?)
// have it configurable which namespaces to white/black list & specific labels in either list to white black list?
// gonna get quite complicated!
// e.g. for tasks, would probably only want to visualise the task and first degree relationships
// that allows different behaviour for different types, but in a generic manner
// we can set up the existing types sensibly, but any added types the user can set themeselves

//todo: (a) either group related nodes of same type together (like neo's browser)
// or (b) have combined data/schema visualiser

// (a) atm just overriding colour, so layout engine doesn't know anything about type
// add property to classattribute of type and change renderer
// or check out all existing owl types and see if can piggy-back or build on any

// (b) e.g. (software developer:jobProfile)--(Tasks)--(coding)

//todo: maxLabelWidth to 180

namespace DFC.ServiceTaxonomy.GraphVisualiser.Controllers
{
    //esco
    // FFE5D4
    // EFC7C2
    // BA9593

    //todo: colour scheme per relationship prefix, so more obvious what's ncs and what's esco
    //todo: add namespace esco|ncs under text in node
    //todo: set relationship width so shows all
    public class ColourScheme
    {
        private string[] _colours;

        public ColourScheme(string[] colours)
        {
            _colours = colours;
        }

        private int _current;

        public string NextColour()
        {
            string colour = _colours[_current++];
            if (_current == _colours.Length)
                _current = 0;
            return colour;
        }
    }

    public class ClassAttribute
    {
        public ClassAttribute(string id, string iri, string baseIri, string label, string comment)
        {
            Id = id;
            Iri = iri;
            BaseIri = baseIri;
            Label = label;
            Comment = comment;
        }

        public string Id { get; set; }
        public string Iri { get; set; }
        public string BaseIri { get; set; }
        public string Label { get; set; }
        public string Comment { get; set; }
        public List<string> Attributes { get; set; } = new List<string>();

        public string StaxBackgroundColour { get; set; }
        public List<string> StaxProperties { get; set; } = new List<string>();

//        public List<string> StaxAttributes { get; set; } = new List<string>();


        //todo: this form required?
        //     ""comment"": {{
        //     ""en"": ""{(string)n.Value.Properties["ncs__Description"]}""
        // }}"
    }


//     public class Neo4jResponse
//     {
//         public string[] Keys { get; set; }
//         //public int Length { get; set; }
//         public Neo4jObject[] _Fields { get; set; }
//         //_fieldLookup
//     }
//
//     public class Neo4jObject
//     {
//         public ulong Id { get; set; }
//
//         // public static Neo4jObject Create()
//         // {
//         //     if (labels != null)
//         //         return
//         // }
//     }
//
//     public class Node : Neo4jObject
//     {
//         public string[] Labels { get; set; }
//         public Dictionary<string, object> Properties { get; set; }
//     }
//
//     public class Relationship : Neo4jObject
//     {
//         public ulong StartNodeId { get; set; }
//         public ulong EndNodeId { get; set; }
//         public string Type { get; set; }
//         public Dictionary<string, object> Properties { get; set; }
//     }
//
//     public class Identity
//     {
//         public uint Low { get; set; }
//         public uint High { get; set; }
//     }
//
//     public class Neo4jObjectJsonConverter : JsonConverter<Neo4jObject>
// {
//     public override bool CanConvert(Type type)
//     {
//         return typeof(Neo4jObject).IsAssignableFrom(type);
//     }
//
//     public override Neo4jObject Read(
//         ref Utf8JsonReader reader,
//         Type typeToConvert,
//         JsonSerializerOptions options)
//     {
//         if (reader.TokenType != JsonTokenType.StartObject)
//             throw new JsonException();
//
//         Identity identity = null;
//         string[] labels = null;
//         string type = null;
//
//         while (reader.Read())
//         {
//             switch (reader.TokenType)
//             {
//                 case JsonTokenType.PropertyName:
//                     string propertyName = reader.GetString();
//                     switch (propertyName)
//                     {
//                         case "identity":
//                             identity = JsonSerializer.Deserialize<Identity>(ref reader);
//                             break;
//                         case "labels":
//                             labels = JsonSerializer.Deserialize<string[]>(ref reader);
//                             break;
//                         case "type":
//                             reader.Read();
//                             type = reader.GetString();
//                             break;
//                         default:
//                             throw new NotSupportedException();
//                     }
//                     break;
//             }
//         }
//
//         //todo: identity combine into uint
//         if (labels != null)
//             return new Node
//             {
//                 Id = identity?.Low ?? throw new JsonException(),
//                 Labels = labels
//             };
//         if (type != null)
//             return new Relationship
//             {
//                 Id = identity?.Low ?? throw new JsonException(),
//                 Type = type
//             };
//
//         throw new JsonException();
//
//         // if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
//         // {
//         //     throw new JsonException();
//         // }
//     }
//
//     public override void Write(
//         Utf8JsonWriter writer,
//         Neo4jObject value,
//         JsonSerializerOptions options)
//     {
//         writer.WriteStartObject();
//
//         switch (value)
//         {
//             case Node node:
//                 JsonSerializer.Serialize(writer, node);
//                 break;
//             case Relationship relationship:
//                 JsonSerializer.Serialize(writer, relationship);
//                 break;
//             default:
//                 throw new NotSupportedException();
//         }
//
//         writer.WriteEndObject();
//     }
// }

    //todo: wwwroot files need to be in this module
    public class VisualiseController : Controller
    {
        private readonly IGraphDatabase _neoGraphDatabase;

        public VisualiseController(IGraphDatabase neoGraphDatabase)
        {
            _neoGraphDatabase = neoGraphDatabase ?? throw new ArgumentNullException(nameof(neoGraphDatabase));
        }

        public async Task<ActionResult> Data([FromQuery] string fetch, [FromQuery] string id)
        {
            long selectedNodeId = -1;
            //Neo4j.Driver.Internal.Types.Node
            var nodes = new Dictionary<long, INode>();
            var relationships = new HashSet<IRelationship>();
            //fetch = "http://nationalcareers.service.gov.uk/jobprofile/1805847e-1ff8-48d7-8215-43ed59446171";
            //fetch = "http://nationalcareers.service.gov.uk/jobprofile/51183020-1d00-4929-9763-666c958c3b37";
            //fetch = "http://nationalcareers.service.gov.uk/jobprofile/69200111-227f-4a97-b7e9-d9d480c83979";
            fetch = "http://nationalcareers.service.gov.uk/jobprofile/fd1d7ab1-5c55-449b-bee4-56deaaacf053";
            var results = await _neoGraphDatabase.RunReadQuery(
                new Query(
//                    $"match (n:ncs__JobProfile {{uri:\"{fetch}\"}})-[r]-(d) return n, d, r"),
                $"match (n:ncs__JobProfile {{uri:\"{fetch}\"}})-[r]-(d) optional match (d)-[r1:esco__relatedEssentialSkill|:esco__relatedOptionalSkill|:ncs__hasUniversityLink|:ncs__hasUniversityRequirement]-(d1) return n, d, r, r1, d1"),

                r =>
                {
                    var otherNode = r["d1"].As<INode>();
                    if (otherNode != null)
                        nodes[otherNode.Id] = otherNode;
                    var otherRelationship = r["r1"].As<IRelationship>();
                    if (otherRelationship != null)
                        relationships.Add(otherRelationship);

                    var sourceNode = r["n"].As<INode>();
                    var destNode = r["d"].As<INode>();
                    relationships.Add(r["r"].As<IRelationship>());

                    selectedNodeId = sourceNode.Id;
                    nodes[sourceNode.Id] = sourceNode;
                    nodes[destNode.Id] = destNode;

                    //todo:
                    return 0;
                });

            var minNodeId = nodes.Keys.Min() - 1;
            var minRelationshipId = relationships.Min(r => r.Id) - 1;

            //from settings global...
            // ""zoom"": ""2.09"",
            // ""translation"": [
            // -1087.15,
            // -750.73
            //     ],


            var response = new StringBuilder(@"{
   ""_comment"": ""Empty ontology for WebVOWL Editor [Additional Information added by WebVOWL Exporter Version: 1.1.7]"",
   ""header"": {
     ""languages"": [
       ""en""
     ],
     ""baseIris"": [
       ""http://www.w3.org/2000/01/rdf-schema""
     ],
     ""iri"": ""https://nationalcareers.service.gov.uk/test/"",
     ""title"": ""test"",
     ""description"": {
       ""en"": ""New ontology description""
     }
   },
   ""namespace"": [],
   ""settings"": {
     ""global"": {
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
   ""class"": [");

            // {
//       ""id"": ""Class1"",
//       ""type"": ""owl:Class""
//     }

            response.AppendJoin(',', nodes.Select(n =>
                $"{{ \"id\": \"Class{n.Key-minNodeId}\", \"type\": \"owl:{(n.Key==selectedNodeId?"equivalent":"")}Class\" }}"));

            response.Append("], \"classAttribute\": [");

            //     {
//       ""id"": ""Class1"",
//       ""iri"": ""https://nationalcareers.service.gov.uk/testJobProfile"",
//       ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//       ""label"": ""JobProfile"",
//         ""pos"": [
//             864.79,
//             688.54
//         ]
//     },

// todo: center current content item?
// mark node label to display in graph??

// different colour
            // "attributes": [
            // "external"
            //     ],

            //description
            // "comment": {
            //     "en": "An automatic tag is a tag that is automatically associated with a resource (e.g. by a tagging system), i.e. it is not entered by a human being."
            // },


            Dictionary<string, string> typeColours = new Dictionary<string, string>();
            var ncsColourScheme = new ColourScheme(new string[] {
                     "#A6EBC9",
                     "#EDFFAB",
                     "#BCE7FD",
                     "#C7DFC5",
                     "#C1DBE3",
                     "#F3C178",
                     "#E2DBBE"
                 });

            var escoColourScheme = new ColourScheme(new string[] {
                "#FFE5D4",
                "#EFC7C2",
                "#BA9593",
                "#F7EDF0",
            });

            const string prefLabel = "skos__prefLabel";

            response.AppendJoin(',', nodes.Select(n =>
            {
//                string type = n.Value.Labels.First(l => l != "Resource");
                string type = n.Value.Labels.First(l => l.StartsWith("ncs__")
                || l == "esco__Occupation"
                || l == "esco__Skill");
                string label;
                if (type.StartsWith("ncs__"))
                {
                    label = (string)n.Value.Properties[prefLabel];
                }
                else
                {
                    label = (string)((List<object>)n.Value.Properties[prefLabel]).First();
                }

                // string comment = n.Value.Properties.ContainsKey("ncs__Description")
                //     ? $@",
                // ""comment"": {{
                //     ""en"": ""{(string)n.Value.Properties["ncs__Description"]}""
                // }}"
                //     : string.Empty;

                string comment = n.Value.Properties.ContainsKey("ncs__Description")
                    ? (string)n.Value.Properties["ncs__Description"]
                    : string.Empty;

                var classAttribute = new ClassAttribute(
                    $"Class{n.Key - minNodeId}",
                    $"https://nationalcareers.service.gov.uk/test/{type}",
                    "https://nationalcareers.service.gov.uk/test/",
                    label,
                    comment);

                if (typeColours.ContainsKey(type))
                {
                    classAttribute.StaxBackgroundColour = typeColours[type];
                }
                else
                {
                    classAttribute.StaxBackgroundColour = typeColours[type] =
                        type.StartsWith("esco__") ? escoColourScheme.NextColour() : ncsColourScheme.NextColour();
                }

                classAttribute.StaxProperties = n.Value.Properties.Where(p => p.Key != prefLabel)
                    .Select(p => $"{p.Key}:{p.Value}").ToList();

                // if (type == "ncs__JobProfile")
                // {
                // //     classAttribute.StaxBackgroundColour = staxBackgroundColour;
                // classAttribute.Attributes.Add("primary");
                // //     // classAttribute.Attributes.Add("external");
                // //     // classAttribute.StaxAttributes.Add("primary");
                // }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                return JsonSerializer.Serialize(classAttribute, jsonOptions);

//                 return $@"{{
//        ""id"": ""Class{n.Key - minNodeId}"",
//        ""iri"": ""https://nationalcareers.service.gov.uk/test/{type}"",
//        ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//        ""label"": ""{label}""
//        {comment}
// }}";
            }));

        response.Append("],\"property\": [");

            // {
            //     ""id"": ""objectProperty1"",
            //     ""type"": ""owl:ObjectProperty""
            // }

            response.AppendJoin(',', relationships.Select(r => $@"{{
            ""id"": ""objectProperty{r.Id - minRelationshipId}"",
            ""type"": ""owl:ObjectProperty""}}"));

            response.Append("], \"propertyAttribute\": [");

            //     {
//       ""id"": ""objectProperty1"",
//       ""iri"": ""https://nationalcareers.service.gov.uk/testhasOccupation"",
//       ""baseIri"": ""https://nationalcareers.service.gov.uk/test"",
//       ""label"": ""hasOccupation"",
//       ""attributes"": [
//         ""object""
//       ],
//       ""domain"": ""Class1"",
//       ""range"": ""Class2""
//     }

            response.AppendJoin(',', relationships.Select(r => $@"{{
       ""id"": ""objectProperty{r.Id - minRelationshipId}"",
       ""iri"": ""https://nationalcareers.service.gov.uk/test/{r.Type}"",
       ""baseIri"": ""https://nationalcareers.service.gov.uk/test"",
       ""label"": ""{r.Type}"",
       ""attributes"": [
         ""object""
       ],
       ""domain"": ""Class{r.StartNodeId - minNodeId}"",
       ""range"": ""Class{r.EndNodeId - minNodeId}""
}}"));

            response.Append("]}");

            return Content(response.ToString(), "application/json");
        }

//          public ActionResult Data([FromQuery]string fetch)
//          {
//              return Content(@"{
//   ""_comment"": ""Empty ontology for WebVOWL Editor [Additional Information added by WebVOWL Exporter Version: 1.1.7]"",
//   ""header"": {
//     ""languages"": [
//       ""en""
//     ],
//     ""baseIris"": [
//       ""http://www.w3.org/2000/01/rdf-schema""
//     ],
//     ""iri"": ""https://nationalcareers.service.gov.uk/test/"",
//     ""title"": ""test"",
//     ""description"": {
//       ""en"": ""New ontology description""
//     }
//   },
//   ""namespace"": [],
//   ""settings"": {
//     ""global"": {
//       ""zoom"": ""2.09"",
//       ""translation"": [
//         -1087.15,
//         -750.73
//       ],
//       ""paused"": false
//     },
//     ""gravity"": {
//       ""classDistance"": 200,
//       ""datatypeDistance"": 120
//     },
//     ""filter"": {
//       ""checkBox"": [
//         {
//           ""id"": ""datatypeFilterCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""objectPropertyFilterCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""subclassFilterCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""disjointFilterCheckbox"",
//           ""checked"": true
//         },
//         {
//           ""id"": ""setoperatorFilterCheckbox"",
//           ""checked"": false
//         }
//       ],
//       ""degreeSliderValue"": ""0""
//     },
//     ""modes"": {
//       ""checkBox"": [
//         {
//           ""id"": ""pickandpinModuleCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""nodescalingModuleCheckbox"",
//           ""checked"": true
//         },
//         {
//           ""id"": ""compactnotationModuleCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""colorexternalsModuleCheckbox"",
//           ""checked"": true
//         }
//       ],
//       ""colorSwitchState"": false
//     }
//   },
//   ""class"": [
//     {
//       ""id"": ""Class1"",
//       ""type"": ""owl:Class""
//     },
//     {
//       ""id"": ""Class2"",
//       ""type"": ""owl:Class""
//     },
//     {
//       ""id"": ""Class3"",
//       ""type"": ""owl:Class""
//     }
//   ],
//   ""classAttribute"": [
//     {
//       ""id"": ""Class1"",
//       ""iri"": ""https://nationalcareers.service.gov.uk/test/ncs__OtherRequirement"",
//       ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//       ""label"": ""ncs__OtherRequirement""
//     },
//     {
//       ""id"": ""Class2"",
//       ""iri"": ""https://nationalcareers.service.gov.uk/test/ncs__JobProfile"",
//       ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//       ""label"": ""ncs__JobProfile""
//     },
//     {
//       ""id"": ""Class3"",
//       ""iri"": ""https://nationalcareers.service.gov.uk/test/ncs__SOCCode"",
//       ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//       ""label"": ""ncs__SOCCode""
//     }
//   ],
//   ""property"": [
//     {
//       ""id"": ""objectProperty1"",
//       ""type"": ""owl:ObjectProperty""
//     },
//     {
//       ""id"": ""objectProperty2"",
//       ""type"": ""owl:ObjectProperty""
//     }
//   ],
//   ""propertyAttribute"": [
//     {
//       ""id"": ""objectProperty1"",
//       ""iri"": ""https://nationalcareers.service.gov.uk/test/ncs__hasSocCode"",
//       ""baseIri"": ""https://nationalcareers.service.gov.uk/test"",
//       ""label"": ""ncs__hasSocCode"",
//       ""attributes"": [
//         ""object""
//       ],
//       ""domain"": ""Class2"",
//       ""range"": ""Class3""
//     },
//     {
//       ""id"": ""objectProperty2"",
//       ""iri"": ""https://nationalcareers.service.gov.uk/test/ncs__hasWitOtherRequirement"",
//       ""baseIri"": ""https://nationalcareers.service.gov.uk/test"",
//       ""label"": ""ncs__hasWitOtherRequirement"",
//       ""attributes"": [
//         ""object""
//       ],
//       ""domain"": ""Class2"",
//       ""range"": ""Class1""
//     }
//   ]
// }", "application/json");
//          }

        // public ActionResult Data([FromQuery] string fetch)
        // {
        //     return Content(@""
        //         , "application/json");
        // }

//         public ActionResult Data([FromQuery] string fetch)
//         {
//             return Content(@"{
//   ""_comment"": ""Empty ontology for WebVOWL Editor [Additional Information added by WebVOWL Exporter Version: 1.1.7]"",
//   ""header"": {
//     ""languages"": [
//       ""en""
//     ],
//     ""baseIris"": [
//       ""http://www.w3.org/2000/01/rdf-schema""
//     ],
//     ""iri"": ""http://visualdataweb.org/newOntology/"",
//     ""title"": {
//       ""en"": ""New ontology""
//     },
//     ""description"": {
//       ""en"": ""New ontology description""
//     }
//   },
//   ""namespace"": [],
//   ""settings"": {
//     ""global"": {
//       ""zoom"": ""0.89"",
//       ""translation"": [
//         378.1,
//         44.57
//       ],
//       ""paused"": false
//     },
//     ""gravity"": {
//       ""classDistance"": 200,
//       ""datatypeDistance"": 120
//     },
//     ""filter"": {
//       ""checkBox"": [
//         {
//           ""id"": ""datatypeFilterCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""objectPropertyFilterCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""subclassFilterCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""disjointFilterCheckbox"",
//           ""checked"": true
//         },
//         {
//           ""id"": ""setoperatorFilterCheckbox"",
//           ""checked"": false
//         }
//       ],
//       ""degreeSliderValue"": ""0""
//     },
//     ""modes"": {
//       ""checkBox"": [
//         {
//           ""id"": ""pickandpinModuleCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""nodescalingModuleCheckbox"",
//           ""checked"": true
//         },
//         {
//           ""id"": ""compactnotationModuleCheckbox"",
//           ""checked"": false
//         },
//         {
//           ""id"": ""colorexternalsModuleCheckbox"",
//           ""checked"": true
//         }
//       ],
//       ""colorSwitchState"": false
//     }
//   },
//   ""class"": [
//     {
//       ""id"": ""Class1"",
//       ""type"": ""owl:Class""
//     },
//     {
//       ""id"": ""Class2"",
//       ""type"": ""owl:Class""
//     },
//     {
//       ""id"": ""Class3"",
//       ""type"": ""owl:Class""
//     }
//   ],
//   ""classAttribute"": [
//     {
//       ""id"": ""Class1"",
//       ""iri"": ""http://visualdataweb.org/newOntology/JobProfile"",
//       ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//       ""label"": ""JobProfile"",
//       ""pos"": [
//         439.09,
//         194.5
//       ]
//     },
//     {
//       ""id"": ""Class2"",
//       ""iri"": ""http://visualdataweb.org/newOntology/SOCCode"",
//       ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//       ""label"": ""SOCCode"",
//       ""pos"": [
//         751.58,
//         327.24
//       ]
//     },
//     {
//       ""id"": ""Class3"",
//       ""iri"": ""http://visualdataweb.org/newOntology/OtherRequirement"",
//       ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//       ""label"": ""OtherRequirement"",
//       ""pos"": [
//         434.42,
//         533.04
//       ]
//     }
//   ],
//   ""property"": [
//     {
//       ""id"": ""objectProperty1"",
//       ""type"": ""owl:ObjectProperty""
//     },
//     {
//       ""id"": ""objectProperty3"",
//       ""type"": ""owl:ObjectProperty""
//     }
//   ],
//   ""propertyAttribute"": [
//     {
//       ""id"": ""objectProperty1"",
//       ""iri"": ""http://visualdataweb.org/newOntology/hasSOCCode"",
//       ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//       ""label"": ""hasSOCCode"",
//       ""attributes"": [
//         ""object""
//       ],
//       ""domain"": ""Class1"",
//       ""range"": ""Class2"",
//       ""pos"": [
//         595.34,
//         260.87
//       ]
//     },
//     {
//       ""id"": ""objectProperty3"",
//       ""iri"": ""http://visualdataweb.org/newOntology/hasOtherRequirement"",
//       ""baseIri"": ""http://visualdataweb.org/newOntology/"",
//       ""label"": ""hasOtherRequirement"",
//       ""attributes"": [
//         ""object""
//       ],
//       ""domain"": ""Class1"",
//       ""range"": ""Class3"",
//       ""pos"": [
//         436.76,
//         363.77
//       ]
//     }
//   ]
// }"
//                 , "application/json");
//         }
    }
}
