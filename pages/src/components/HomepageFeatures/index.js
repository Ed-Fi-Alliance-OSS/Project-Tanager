import Heading from "@theme/Heading";
import Link from "@docusaurus/Link";
import styles from "./styles.module.css";

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          <div className="col col--offset-1 col--5">
            <Heading as="h2">Project Documents</Heading>
            <ul>
              <li>
                <Link to="docs/intro">Introducing Project Tanager</Link>:
                motivation and architectural vision
              </li>
              <li>
                <Link to="docs/faq">
                  Ed-Fi ODS/API and Data Management Service FAQ
                </Link>
              </li>
              <li>
                <Link to="docs/design">Architecture and Design</Link>
              </li>
            </ul>
            <Heading as="h2"> Products</Heading>
            <p>
              Tanager is a <i>project</i>, these are the <i>products</i>:
            </p>
            <ul>
              <li>
                <Link to="docs/">Data Management Service</Link>
              </li>
              <li>Configuration Service - pending</li>
            </ul>
          </div>
          <div className="col col--5">
            <Heading as="h2"> Boilerplate</Heading>
            <ul>
              <li>
                <Link to="docs/">How to Contribute</Link>
              </li>
              <li>
                <Link to="docs/">Contributor Code of Conduct</Link>
              </li>
              <li>
                <Link to="docs/">List of Contributors</Link>
              </li>
              <li>
                <Link to="docs/">Copyright and License Notices</Link>
              </li>
              <li>
                <Link to="docs/">License</Link>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
}
