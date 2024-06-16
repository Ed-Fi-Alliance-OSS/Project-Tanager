import Heading from "@theme/Heading";

export default function About() {
  return (
    <section>
      <div className="container">
        <div className="row">
          <div className="col col--5 col--offset-1">
            <Heading as="h1">About</Heading>
            <p>
              Design and reference documentation for the Ed-Fi Alliance's
              "Project Tanager", which is building the next generation of the
              <a href="/">Ed-Fi Alliance Technology Suite</a>.
            </p>
            <Heading as="h2">What is a tanager?</Heading>
            <p>
              "Tanagers" are a class of new world birds, including the Scarlet
              Tanager pictured above. The Scarlet Tanager breeds across much of
              eastern North America and winters in northwestern South America.
            </p>
            <p>
              This photo was taken by&nbsp;
              <a href="https://www.inaturalist.org/observations/193251982">
                Adam Jackson, with no rights reserved
              </a>.
            </p>
          </div>
          <div className="col col--5">
            <img src="img/scarlet-tanager_by_adam-jackson_no-rights-reserved.jpg" />
          </div>
        </div>
      </div>
    </section>
  );
}
