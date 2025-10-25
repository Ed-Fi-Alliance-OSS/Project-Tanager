// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Shouldly;

namespace JsonSchemaShredder.Tests;

public class DbEntityNameTests
{
  [TestCase("students", "Student")]
  [TestCase("people", "Person")]
  [TestCase("batteries", "Battery")]
  [TestCase("classes", "Class")]
  [TestCase("wishes", "Wish")]
  [TestCase("boxes", "Box")]
  [TestCase("quizzes", "Quiz")]
  [TestCase("addresses", "Address")]
  [TestCase("address", "Address")]
  // Compound words
  [TestCase("assessmentBatteries", "AssessmentBattery")]
  [TestCase("studentAddress", "StudentAddress")]
  [TestCase("PopQuizzes", "PopQuiz")]
  [TestCase("employeeClasses", "EmployeeClass")]
  public void CorrectlyNormalizeNames(string plural, string expectedSingular)
  {
    DbEntityName.Normalize(plural).ShouldBe(expectedSingular);
  }

  /* Not going to hard-code these edge cases for now:

    Here are some education-related words with unusual plural forms:
    1. Words ending in “is”

    thesis → theses
    analysis → analyses
    hypothesis → hypotheses

    2. Words ending in “um”

    curriculum → curricula (or curriculums in modern usage)
    datum → data
    medium → media (in academic contexts)

    3. Words ending in “on”

    criterion → criteria
    phenomenon → phenomena

    4. Words borrowed from Latin/Greek

    alumnus → alumni (male or mixed group)
    alumna → alumnae (female group)
    syllabus → syllabi (or syllabuses)
  */
}
